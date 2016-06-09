library("NLP")
library("tau")
library("tm")
library("MASS")
library("nnet")

Main<- function(){
  #start het programma
  #head(description)
  if(checkFunc())
    ReadInfunc()

}
equallength<-function(x,y)
{
  x==y
}

all.queryterms <- function (queries,docs)
{
  a<-sapply(queries, method="string",n=1L, textcnt)
  b<-sapply(sapply(docs, method="string",n=1L, textcnt), names)
  c<-mapply(intersect,sapply(a,names),b)
  feature<- sapply((mapply(equallength,sapply(a,length),sapply(c,length))), as.numeric)
  #n <- length(queries)
  #feature <- vector(length=n)
  #for(i in 1:n){
  #  query <- queries[i]
  #  document <- docs[i]
  #  a <- textcnt(query,method="string",n=1L)
  #  b <- textcnt(document,method="string",n=1L)
  #  c <- intersect(names(a), names(b))
  #  feature[i] <- as.numeric(length(a)==length(c))}
  feature
}

test<-function(){
  summary(all.queryterms(queries$search_term, queries$product_title))
}






ReadInfunc<- function(){
  descriptions<<-read.csv(file="product_descriptions.csv", row.names = 1, stringsAsFactors = FALSE)
  queries<<-read.csv(file="query_product.csv", row.names = 1, stringsAsFactors = FALSE)
}

checkFunc<-function(){
  !exists("queries")||!exists("descriptions")
}