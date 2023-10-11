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
  name                  = "ghcn-raw"
  resource_group_name   = azurerm_cosmosdb_account.cosmos.resource_group_name
  account_name          = azurerm_cosmosdb_account.cosmos.name
  database_name         = azurerm_cosmosdb_sql_database.db.name
  partition_key_path    = "/definition/id"
  partition_key_version = 1
  throughput            = 400

  indexing_policy {
    indexing_mode = "consistent"

    included_path {
      path = "/*"
    }

    included_path {
      path = "/included/?"
    }

    excluded_path {
      path = "/excluded/?"
    }
  }

  unique_key {
    paths = ["/definition/idlong", "/definition/idshort"]
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
  principal_id         = module.data_factory.identity
}

resource "azurerm_storage_container" "ghcn" {
  name                  = "ghcn"
  storage_account_name  = azurerm_storage_account.st.name
  container_access_type = "private"
}

module "data_factory" {
  source = "./modules/data_factory"

  location                          = data.azurerm_resource_group.rg.location
  resource_group_name               = data.azurerm_resource_group.rg.name
  storage_account_connection_string = azurerm_storage_account.st.primary_connection_string
  cosmosdb_name                     = azurerm_cosmosdb_account.cosmos.name
}

resource "azurerm_data_factory_pipeline" "ghcn" {
  name            = "ghcn"
  data_factory_id = module.data_factory.id
}