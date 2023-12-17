resource "azurerm_DataFactoryTest" "factory" {
  name                = "devadfnoaa"
  location            = var.location
  resource_group_name = var.resource_group_name

  identity {
    type = "SystemAssigned"
  }
}


##################################################################
#                        Linked Services                         #
##################################################################

# resource "azurerm_DataFactoryTest_linked_service_azure_blob_storage" "ghcn_iam" {
#   name              = "ghcn_blob_storage"
#   DataFactoryTest_id   = azurerm_DataFactoryTest.factory.id
#   connection_string = var.storage_account_connection_string
# }

resource "azurerm_DataFactoryTest_linked_service_azure_blob_storage" "ghcn" {
  name                       = "ghcn_blob_storage"
  DataFactoryTest_id            = azurerm_DataFactoryTest.factory.id
  connection_string_insecure = var.storage_account_connection_string
  storage_kind               = "BlobStorage"
  use_managed_identity       = true
}

resource "azurerm_DataFactoryTest_linked_service_cosmosdb" "ghcn" {
  name              = "ghcn_cosmosdb"
  DataFactoryTest_id   = azurerm_DataFactoryTest.factory.id
  connection_string = var.cosmos_connection_string
  database          = "ghcn"
}

resource "azurerm_DataFactoryTest_linked_custom_service" "ghcn_http" {
  name                 = "ghcn_http"
  DataFactoryTest_id      = azurerm_DataFactoryTest.factory.id
  type                 = "HttpServer"
  type_properties_json = <<JSON
{
    "url": "https://www.ncei.noaa.gov/",
    "enableServerCertificateValidation": true,
    "authenticationType": "Anonymous"
}
JSON

  parameters = {}

  annotations = []
}

##################################################################
#                         Datasets                               #
##################################################################

resource "azurerm_DataFactoryTest_dataset_http" "ghcn" {
  name                = "ghcn_by_year"
  DataFactoryTest_id     = azurerm_DataFactoryTest.factory.id
  linked_service_name = azurerm_DataFactoryTest_linked_custom_service.ghcn_http.name

  relative_url   = "@concat('/pub/data/ghcn/daily/by_year/', dataset().year, '.csv.gz')"
  request_method = "GET"

  parameters = {
    "year" = ""
  }
}

resource "azurerm_DataFactoryTest_dataset_delimited_text" "ghcn_compressed" {
  name                = "ghcn_compressed"
  DataFactoryTest_id     = azurerm_DataFactoryTest.factory.id
  linked_service_name = azurerm_DataFactoryTest_linked_service_azure_blob_storage.ghcn.name
  compression_codec   = "gzip"
  column_delimiter    = ","
  row_delimiter       = "\n"

  azure_blob_storage_location {
    container                = var.storage_account_container
    path                     = "compressed"
    dynamic_filename_enabled = true
    filename                 = "@concat(dataset().year, '.csv.gz')"
  }

  parameters = {
    year = ""
  }
}

resource "azurerm_DataFactoryTest_dataset_delimited_text" "ghcn_extract" {
  name                = "ghcn_extract"
  DataFactoryTest_id     = azurerm_DataFactoryTest.factory.id
  linked_service_name = azurerm_DataFactoryTest_linked_service_azure_blob_storage.ghcn.name
  column_delimiter    = ","
  row_delimiter       = "\n"

  azure_blob_storage_location {
    container = var.storage_account_container
    path      = "extracted"
  }
}

resource "azurerm_DataFactoryTest_dataset_binary" "ghcn" {
  name                = "ghcn_raw"
  DataFactoryTest_id     = azurerm_DataFactoryTest.factory.id
  linked_service_name = azurerm_DataFactoryTest_linked_service_azure_blob_storage.ghcn.name

  azure_blob_storage_location {
    container = var.storage_account_container
    path      = "compressed"
  }

  parameters = {
    year = ""
  }
}

resource "azurerm_DataFactoryTest_dataset_cosmosdb_sqlapi" "ghcn" {
  name                = "ghcn_cosmos"
  DataFactoryTest_id     = azurerm_DataFactoryTest.factory.id
  linked_service_name = azurerm_DataFactoryTest_linked_service_cosmosdb.ghcn.name

  collection_name = "ghcn-raw"
}

##################################################################
#                         Data Flow                              #
##################################################################

resource "azurerm_DataFactoryTest_data_flow" "example" {
  name            = "cosmos"
  DataFactoryTest_id = azurerm_DataFactoryTest.factory.id

  source {
    name = "source"

    dataset {
      name = azurerm_DataFactoryTest_dataset_delimited_text.ghcn_extract.name
    }
  }

  sink {
    name = "sink"

    dataset {
      name = azurerm_DataFactoryTest_dataset_cosmosdb_sqlapi.ghcn.name
    }
  }

  transformation {
    name = "parse"
  }

  script_lines = [
    "source(allowSchemaDrift: true,",
    "     validateSchema: false,",
    "     ignoreNoFilesFound: false) ~> source",
    "source sink(allowSchemaDrift: true,",
    "     validateSchema: false,",
    "     deletable:false,",
    "     insertable:true,",
    "     updateable:false,",
    "     upsertable:false,",
    "     recreate:true,",
    "     format: 'document',",
    "     partitionKey: ['/_col0_'],",
    "     throughput: 400,",
    "     skipDuplicateMapInputs: true,",
    "     skipDuplicateMapOutputs: true) ~> sink"
  ]
}
