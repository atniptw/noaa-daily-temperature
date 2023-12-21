locals {
  data_factory_name = "devadfnoaa"
}

resource "azurerm_data_factory" "factory" {
  name                = local.data_factory_name
  location            = var.location
  resource_group_name = var.resource_group_name

  identity {
    type = "SystemAssigned"
  }
}

##################################################################
#                        Storage Account                         #
##################################################################

module "storage_account" {
  source = "../storage_account"

  location            = var.location
  resource_group_name = var.resource_group_name
  used_by             = local.data_factory_name
}

resource "azurerm_role_assignment" "example" {
  scope                = module.storage_account.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azurerm_data_factory.factory.identity[0].principal_id
}

resource "azurerm_storage_container" "ghcn" {
  name                  = "ghcn"
  storage_account_name  = module.storage_account.name
  container_access_type = "private"
}

resource "azurerm_storage_management_policy" "example" {
  storage_account_id = module.storage_account.id

  rule {
    name    = "rule1"
    enabled = true
    filters {
      blob_types = ["blockBlob"]
    }
    actions {
      base_blob {
        delete_after_days_since_modification_greater_than = 1
      }
      snapshot {
        delete_after_days_since_creation_greater_than = 1
      }
    }
  }
}


##################################################################
#                        Linked Services                         #
##################################################################

resource "azurerm_data_factory_linked_service_azure_blob_storage" "ghcn" {
  name                       = "ghcn_blob_storage"
  data_factory_id            = azurerm_data_factory.factory.id
  connection_string_insecure = module.storage_account.primary_blob_connection_string
  storage_kind               = "BlobStorage"
  use_managed_identity       = true
}

resource "azurerm_data_factory_linked_service_cosmosdb" "ghcn" {
  name              = "ghcn_cosmosdb"
  data_factory_id   = azurerm_data_factory.factory.id
  connection_string = var.cosmos_connection_string
  database          = "ghcn"
}

resource "azurerm_data_factory_linked_custom_service" "ghcn_http" {
  name                 = "ghcn_http"
  data_factory_id      = azurerm_data_factory.factory.id
  type                 = "HttpServer"
  type_properties_json = <<JSON
{
    "url": "https://www.ncei.noaa.gov/",
    "enableServerCertificateValidation": true,
    "authenticationType": "Anonymous"
}
JSON
}

resource "azurerm_data_factory_linked_service_azure_function" "example" {
  name            = "ghcn_function"
  data_factory_id = azurerm_data_factory.factory.id
  url             = "https://${var.function_app_hostname}"
  key             = var.function_app_key
}

##################################################################
#                         Datasets                               #
##################################################################

resource "azurerm_data_factory_dataset_http" "ghcn" {
  name                = "ghcn_by_year"
  data_factory_id     = azurerm_data_factory.factory.id
  linked_service_name = azurerm_data_factory_linked_custom_service.ghcn_http.name

  relative_url   = "@concat('/pub/data/ghcn/daily/by_year/', dataset().year, '.csv.gz')"
  request_method = "GET"

  parameters = {
    "year" = ""
  }
}

resource "azurerm_data_factory_dataset_delimited_text" "ghcn_compressed" {
  name                = "ghcn_compressed"
  data_factory_id     = azurerm_data_factory.factory.id
  linked_service_name = azurerm_data_factory_linked_service_azure_blob_storage.ghcn.name
  compression_codec   = "gzip"
  column_delimiter    = ","
  row_delimiter       = "\n"

  parameters = {
    filename = ""
  }

  azure_blob_storage_location {
    container                = azurerm_storage_container.ghcn.name
    path                     = "compressed"
    dynamic_filename_enabled = true
    filename                 = "@dataset().filename"
  }

}

resource "azurerm_data_factory_dataset_delimited_text" "ghcn_extract" {
  name                = "ghcn_extract"
  data_factory_id     = azurerm_data_factory.factory.id
  linked_service_name = azurerm_data_factory_linked_service_azure_blob_storage.ghcn.name
  column_delimiter    = ","
  row_delimiter       = "\n"

  azure_blob_storage_location {
    container = azurerm_storage_container.ghcn.name
    path      = "extracted"
  }
}

resource "azurerm_data_factory_dataset_delimited_text" "ghcn_extract2" {
  name                = "DelimitedText2"
  data_factory_id     = azurerm_data_factory.factory.id
  linked_service_name = azurerm_data_factory_linked_service_azure_blob_storage.ghcn.name
  column_delimiter    = ","

  parameters = {
    filename = ""
  }

  azure_blob_storage_location {
    container                = azurerm_storage_container.ghcn.name
    dynamic_filename_enabled = true
    path                     = "extracted"
    filename                 = "@dataset().filename"
  }

}

resource "azurerm_data_factory_dataset_binary" "ghcn" {
  name                = "ghcn_raw"
  data_factory_id     = azurerm_data_factory.factory.id
  linked_service_name = azurerm_data_factory_linked_service_azure_blob_storage.ghcn.name

  azure_blob_storage_location {
    container = azurerm_storage_container.ghcn.name
    path      = "compressed"
  }
}

resource "azurerm_data_factory_dataset_cosmosdb_sqlapi" "ghcn" {
  name                = "ghcn_cosmos"
  data_factory_id     = azurerm_data_factory.factory.id
  linked_service_name = azurerm_data_factory_linked_service_cosmosdb.ghcn.name

  collection_name = "staging"
}

##################################################################
#                         Data Flow                              #
##################################################################

resource "azurerm_data_factory_data_flow" "example" {
  name            = "cosmos"
  data_factory_id = azurerm_data_factory.factory.id

  source {
    name = "source"

    dataset {
      name = azurerm_data_factory_dataset_delimited_text.ghcn_extract2.name
    }
  }

  transformation {
    name = "filter1"
  }
  transformation {
    name = "derivedColumn1"
  }
  transformation {
    name = "select1"
  }

  sink {
    name = "sink"

    dataset {
      name = azurerm_data_factory_dataset_cosmosdb_sqlapi.ghcn.name
    }
  }

  script_lines = [
    "source(output(",
    "          Column_1 as string,",
    "          Column_2 as string,",
    "          Column_3 as string,",
    "          Column_4 as string,",
    "          Column_5 as string,",
    "          Column_6 as string,",
    "          Column_7 as string,",
    "          Column_8 as string",
    "     ),",
    "     allowSchemaDrift: true,",
    "     validateSchema: false,",
    "     ignoreNoFilesFound: false) ~> source",
    "source filter(not(isNull(Column_1)) && not(isNull(Column_2)) && not(isNull(Column_3)) && not(isNull(Column_4))) ~> filter1",
    "filter1 derive(RecordValue = divide(toInteger(Column_4), 10)) ~> derivedColumn1",
    "derivedColumn1 select(mapColumn(",
    "          StationId = Column_1,",
    "          Date = Column_2,",
    "          RecordType = Column_3,",
    "          RecordValue,",
    "          MeasurementFlag = Column_5,",
    "          QualityFlag = Column_6,",
    "          SourceFlag = Column_7,",
    "          ObservationTime = Column_8",
    "     ),",
    "     skipDuplicateMapInputs: true,",
    "     skipDuplicateMapOutputs: true) ~> select1",
    "select1 sink(allowSchemaDrift: true,",
    "     validateSchema: false,",
    "     deletable:false,",
    "     insertable:true,",
    "     updateable:false,",
    "     upsertable:false,",
    "     format: 'document',",
    "     throughput: 400,",
    "     skipDuplicateMapInputs: true,",
    "     skipDuplicateMapOutputs: true) ~> sink"
  ]
}
