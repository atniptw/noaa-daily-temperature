data "azurerm_cosmosdb_account" "example" {
  name                = var.cosmosdb_name
  resource_group_name = var.resource_group_name
}

resource "azurerm_data_factory" "factory" {
  name                = "devadfnoaa"
  location            = var.location
  resource_group_name = var.resource_group_name

  identity {
    type = "SystemAssigned"
  }
}

resource "azurerm_data_factory_linked_service_azure_blob_storage" "ghcn" {
  name              = "ghcn"
  data_factory_id   = azurerm_data_factory.factory.id
  connection_string = var.storage_account_connection_string
}

resource "azurerm_data_factory_linked_service_cosmosdb" "grcn" {
  name             = "ghcn"
  data_factory_id  = azurerm_data_factory.factory.id
  account_endpoint = data.azurerm_cosmosdb_account.example.endpoint
  account_key      = data.azurerm_cosmosdb_account.example.primary_key
  database         = "foo"

}
