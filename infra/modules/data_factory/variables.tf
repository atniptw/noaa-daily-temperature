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

variable "storage_account_container" {
  type = string
}

variable "cosmosdb_name" {
  type = string
}

variable "cosmos_connection_string" {
  type      = string
  sensitive = true
}

variable "function_app_hostname" {
  type = string
}

variable "function_app_key" {
  type = string
}
