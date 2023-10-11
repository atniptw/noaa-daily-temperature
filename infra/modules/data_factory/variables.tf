variable "location" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "storage_account_connection_string" {
  type      = string
  sensitive = true
}

variable "cosmosdb_name" {
  type = string
}

variable "cosmosdb_endpoint" {
  type = string
}

variable "cosmosdb_primary_key" {
  type = string
}