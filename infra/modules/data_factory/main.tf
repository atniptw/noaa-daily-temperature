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

resource "azurerm_data_factory_linked_service_cosmosdb" "grcn" {
  name             = "ghcn_cosmosdb"
  data_factory_id  = azurerm_data_factory.factory.id
  account_endpoint = var.cosmosdb_endpoint
  account_key      = var.cosmosdb_primary_key
  database         = "ghcn"
}
