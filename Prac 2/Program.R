library("NLP")
library("tau")
library("tm")
library("MASS")
library("nnet")

Main<- function(){
  #start het programma
  readQueryProduct()
}

readQueryProduct <- function() {
  query_product.dat <- read.csv("query_product.csv", stringsAsFactors=FALSE)
  str(query_product.dat)
  query_product.dat[query_product.dat[,1] == 9]
}

stringprint <-function(x){
  x
}
stringprint("test")