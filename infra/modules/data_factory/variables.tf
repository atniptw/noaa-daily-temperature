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