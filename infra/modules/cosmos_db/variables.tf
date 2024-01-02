variable "resource_group_name" {
  type = string
}

variable "db_name" {
  type = string
}

variable "cosmosdb_account_name" {
  type = string
}

variable "ttl" {
  type    = number
  default = -1
}