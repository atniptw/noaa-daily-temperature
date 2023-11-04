resource "azurerm_data_factory" "factory" {
  name                = "devadfnoaa"
  location            = var.location
  resource_group_name = var.resource_group_name

  identity {
    type = "SystemAssigned"
  }
}

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
  folder              = "compressed"

  azure_blob_storage_location {
    container = var.storage_account_container
  }
}

resource "azurerm_data_factory_dataset_delimited_text" "ghcn_extract" {
  name                = "ghcn_extract"
  data_factory_id     = azurerm_data_factory.factory.id
  linked_service_name = azurerm_data_factory_linked_service_azure_blob_storage.ghcn.name
  column_delimiter    = ","
  folder              = "extract"

  azure_blob_storage_location {
    container = var.storage_account_container
  }
}
