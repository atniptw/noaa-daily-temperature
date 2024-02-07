resource "azurerm_cosmosdb_sql_database" "db" {
  name                = var.db_name
  resource_group_name = var.resource_group_name
  account_name        = var.cosmosdb_account_name
}

resource "azurerm_cosmosdb_sql_container" "ghcn" {
  name                  = "ghcn"
  resource_group_name   = var.resource_group_name
  account_name          = var.cosmosdb_account_name
  database_name         = azurerm_cosmosdb_sql_database.db.name
  partition_key_path    = "/date"
  partition_key_version = 1
  default_ttl           = var.ttl

  autoscale_settings {
    max_throughput = 1000
  }

  indexing_policy {
    indexing_mode = "consistent"

    included_path {
      path = "/date"
    }

    included_path {
      path = "/stationId"
    }

    spatial_index {
      path = "/location/*"
    }
  }
}
