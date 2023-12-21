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
    tenant_id = data.azurerm_client_config.current.tenant_id
    object_id = module.data_factory.identity

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

  depends_on = [module.data_factory]
}

resource "azurerm_cosmosdb_account" "cosmos" {
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

resource "azurerm_cosmosdb_sql_database" "db" {
  name                = "ghcn"
  resource_group_name = azurerm_cosmosdb_account.cosmos.resource_group_name
  account_name        = azurerm_cosmosdb_account.cosmos.name
}

resource "azurerm_cosmosdb_sql_container" "ghcn" {
  name                  = "staging"
  resource_group_name   = azurerm_cosmosdb_account.cosmos.resource_group_name
  account_name          = azurerm_cosmosdb_account.cosmos.name
  database_name         = azurerm_cosmosdb_sql_database.db.name
  partition_key_path    = "/Date"
  partition_key_version = 1
  throughput            = 400
  default_ttl           = 3600

  indexing_policy {
    indexing_mode = "consistent"

    included_path {
      path = "/*"
    }
  }
}

module "function_app" {
  source = "./modules/function_app"

  location            = data.azurerm_resource_group.rg.location
  resource_group_name = data.azurerm_resource_group.rg.name
}

module "data_factory" {
  source = "./modules/data_factory"

  location                 = data.azurerm_resource_group.rg.location
  resource_group_name      = data.azurerm_resource_group.rg.name
  cosmosdb_name            = azurerm_cosmosdb_account.cosmos.name
  cosmos_connection_string = azurerm_cosmosdb_account.cosmos.primary_sql_connection_string
  function_app_hostname    = module.function_app.hostname
  function_app_key         = module.function_app.function_key
}

resource "azurerm_data_factory_pipeline" "ghcn" {
  name            = "ghcn"
  data_factory_id = module.data_factory.id

  activities_json = templatefile("${path.module}/GHCN_Pipeline.json", {})
}
