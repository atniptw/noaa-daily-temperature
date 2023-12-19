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
  container_access_type = "container"
}


##################################################################
#                        Linked Services                         #
##################################################################

# resource "azurerm_data_factory_linked_service_azure_blob_storage" "ghcn_iam" {
#   name              = "ghcn_blob_storage"
#   data_factory_id   = azurerm_data_factory.factory.id
#   connection_string = var.storage_account_connection_string
# }

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

  parameters = {}

  annotations = []
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

  azure_blob_storage_location {
    container                = azurerm_storage_container.ghcn.name
    path                     = "compressed"
    dynamic_filename_enabled = true
    filename                 = "@concat(dataset().year, '.csv.gz')"
  }

  parameters = {
    year = ""
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

resource "azurerm_data_factory_dataset_binary" "ghcn" {
  name                = "ghcn_raw"
  data_factory_id     = azurerm_data_factory.factory.id
  linked_service_name = azurerm_data_factory_linked_service_azure_blob_storage.ghcn.name

  azure_blob_storage_location {
    container = azurerm_storage_container.ghcn.name
    path      = "compressed"
  }

  parameters = {
    year = ""
  }
}

resource "azurerm_data_factory_dataset_cosmosdb_sqlapi" "ghcn" {
  name                = "ghcn_cosmos"
  data_factory_id     = azurerm_data_factory.factory.id
  linked_service_name = azurerm_data_factory_linked_service_cosmosdb.ghcn.name

  collection_name = "ghcn-raw"
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
      name = azurerm_data_factory_dataset_delimited_text.ghcn_extract.name
    }
  }

  sink {
    name = "sink"

    dataset {
      name = azurerm_data_factory_dataset_cosmosdb_sqlapi.ghcn.name
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
