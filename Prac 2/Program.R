library("NLP")
library("tau")
library("tm")
library("MASS")
library("nnet")
library("e1071") #heeft naive bayes functie kan handig zijn(?)


#heb werkelijk geen idee hoe we moeten beginnen tbh.
Main<- function(){
  #start het programma
  readQueryProduct()
  
  
  #head(description)
  if(checkFunc())
    ReadInfunc()
  allterms<- all.queryterms(queries$search_term,queries$product_title)
  
  #werkelijk geen idee wat ik hier doe maar dit komt uit de slides
  #zit ook nog te denken hoe we dus gaan gokken
  qp.dat<-data.frame(relevance=queries$relevance,allterms=allterms)
  tr.index<-sample(length(queries$search_term),50000)
  qp.lm<-lm(relevance~allterms,data=qp.dat[tr.index,])
  summary(qp.lm)
}

readQueryProduct <- function() {
  query_product.dat <- read.csv("query_product.csv", stringsAsFactors=FALSE)
  str(query_product.dat)
  query_product.dat[query_product.dat[,1] == 9]
}

#functie om lengtes te vergelijken
equallength<-function(x,y)
{
  x==y
}

#kijken of alle woorden voorkomen in de titel(en ook geen anderen heeft)
all.queryterms <- function (queries,docs)
{
  a<-sapply(queries, method="string",n=1L, textcnt)
  b<-sapply(sapply(docs, method="string",n=1L, textcnt), names)
  c<-mapply(intersect,sapply(a,names),b)
  feature<- sapply((mapply(function(x,y){length(x)==length(y)},a,c)), as.numeric)
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
