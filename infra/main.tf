data "azurerm_resource_group" "rg" {
  name = var.resource_group
}

data "azurerm_client_config" "current" {}

resource "azurerm_key_vault" "kv" {
  name                        = "devkvnoaa"
  location                    = data.azurerm_resource_group.rg.location
  resource_group_name         = data.azurerm_resource_group.rg.name
  enabled_for_disk_encryption = true
  tenant_id                   = data.azurerm_client_config.current.tenant_id
  soft_delete_retention_days  = 7
  purge_protection_enabled    = false

  sku_name = "standard"

  access_policy {
    tenant_id = data.azurerm_client_config.current.tenant_id
    object_id = data.azurerm_client_config.current.object_id

    key_permissions = [
      "Get",
    ]

    secret_permissions = [
      "Backup",
      "Delete",
      "Get",
      "List",
      "Purge",
      "Recover",
      "Restore",
      "Set"
    ]

    storage_permissions = [
      "Get",
    ]
  }

  access_policy {
    tenant_id = azurerm_data_factory.factory.identity[0].tenant_id
    object_id = azurerm_data_factory.factory.identity[0].principal_id

    key_permissions = [
      "Get",
    ]

    secret_permissions = [
      "Get",
      "List",
    ]

    storage_permissions = [
      "Get",
    ]
  }

  depends_on = [azurerm_data_factory.factory]
}

resource "azurerm_data_factory" "factory" {
  name                = "devadfnoaa"
  location            = data.azurerm_resource_group.rg.location
  resource_group_name = data.azurerm_resource_group.rg.name

  identity {
    type = "SystemAssigned"
  }
}

resource "azurerm_cosmosdb_account" "db" {
  name                = "devcosmosnoaa01"
  location            = data.azurerm_resource_group.rg.location
  resource_group_name = data.azurerm_resource_group.rg.name
  offer_type          = "Standard"
  kind                = "GlobalDocumentDB"

  enable_automatic_failover = false
  consistency_policy {
    consistency_level = "Eventual"
  }

  geo_location {
    location          = data.azurerm_resource_group.rg.location
    failover_priority = 0
  }
}

resource "azurerm_storage_account" "st" {
  name                     = "devstnoaa02"
  location                 = data.azurerm_resource_group.rg.location
  resource_group_name      = data.azurerm_resource_group.rg.name
  account_tier             = "Standard"
  account_replication_type = "LRS"

  tags = {
    environment = var.environment
  }
}

resource "azurerm_role_assignment" "example" {
  scope                = azurerm_storage_account.st.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azurerm_data_factory.factory.identity[0].principal_id
}

resource "azurerm_storage_container" "example" {
  name                  = "GHCN"
  storage_account_name  = azurerm_storage_account.st.name
  container_access_type = "private"
}