resource "azurerm_data_factory" "factory" {
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

resource "azurerm_data_factory_linked_service_azure_blob_storage" "ghcn" {
  name              = "ghcn_blob_storage"
  data_factory_id   = azurerm_data_factory.factory.id
  connection_string = var.storage_account_connection_string
}

resource "azurerm_data_factory_linked_service_cosmosdb" "ghcn" {
  name             = "ghcn_cosmosdb"
  data_factory_id  = azurerm_data_factory.factory.id
  account_endpoint = var.cosmosdb_endpoint
  account_key      = var.cosmosdb_primary_key
  database         = "ghcn"
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
    "year" = "2023"
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
    container                = var.storage_account_container
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
    container = var.storage_account_container
    path      = "extracted"
  }
}

resource "azurerm_data_factory_dataset_binary" "ghcn" {
  name                = "ghcn_raw"
  data_factory_id     = azurerm_data_factory.factory.id
  linked_service_name = azurerm_data_factory_linked_service_azure_blob_storage.ghcn.name

  azure_blob_storage_location {
    container = var.storage_account_container
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

  collection_name = "ghcd-raw"
}

##################################################################
#                         Data Flow                              #
##################################################################

resource "azurerm_data_factory_data_flow" "example" {
  name            = "cosmos"
  data_factory_id = azurerm_data_factory.factory.id

  source {
    name = "ghcn_storage"

    dataset {
      name = azurerm_data_factory_dataset_delimited_text.ghcn_extract.name
    }
  }

  sink {
    name = "ghcn_cosmos"

    dataset {
      name = azurerm_data_factory_dataset_cosmosdb_sqlapi.ghcn.name
    }
  }

  script = <<EOT
source(allowSchemaDrift: true,
	validateSchema: false,
	ignoreNoFilesFound: false) ~> ghcn_storage
ghcn_storage derive(stationId = toString(byPosition(1)),
		date = toString(byPosition(2)),
		recordType = toString(byPosition(3)),
		recordValue = toString(byPosition(4)),
		measurementFlag = toString(byPosition(5)),
		qualityFlag = toString(byPosition(6)),
		sourceFlag = toString(byPosition(7)),
		observationTime = toString(byPosition(8))) ~> derivedColumn1
derivedColumn1 sink(allowSchemaDrift: true,
	validateSchema: false,
	deletable:false,
	insertable:true,
	updateable:false,
	upsertable:false,
	format: 'document',
	skipDuplicateMapInputs: true,
	skipDuplicateMapOutputs: true) ~> ghcn_cosmos
EOT
}
