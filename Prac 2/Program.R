library("NLP")
library("tau")
library("tm")
library("MASS")
library("nnet")
library("vecsets")
library("e1071") #heeft naive bayes functie kan handig zijn(?)

#Alleen voor Lukas:
#setwd("C:/Users/Lukas/Desktop/School/DATA/IRA/Prac 2")

# idee 1: getallen matchen
# idee 2: afkortingen matchen
# idee 3: qf of idf ofz

#heb werkelijk geen idee hoe we moeten beginnen tbh.
Main<- function(){
  #start het programma
  #readQueryProduct()
  
  
  #head(description)
  if(checkFunc())
    ReadInfunc()
  #allterms<- all.queryterms(queries$search_term,queries$product_title)
  alltermsdesc <- all.querytermsdesc(queries$search_term, queries$product_uid, descriptions$product_uid, descriptions$product_description)
  
  #werkelijk geen idee wat ik hier doe maar dit komt uit de slides
  #zit ook nog te denken hoe we dus gaan gokken
  #qp.dat<-data.frame(relevance=queries$relevance,allterms=allterms)
  #tr.index<-sample(length(queries$search_term),length(queries)*2/3)
  #qp.lm<-lm(relevance~allterms,data=qp.dat[tr.index,])
  #summary(qp.lm)
  alltermsdesc

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
all.queryterms <- function (queries, docs)
{
  a<-sapply(queries, method="string",n=1L, textcnt)
  b<-sapply(sapply(docs, method="string",n=1L, textcnt), names)
  c<-mapply(intersect,sapply(a,names),b)
  feature<- sapply((mapply(function(x,y){length(x)==length(y)},a,c)), as.numeric)
  feature
}

getProductDescFromQuery <- function(id) {
  descriptions[toString(queries[toString(id), "product_uid"]),]
}

all.querytermsdesc <- function(queries, productid, descriptid, descript) {
  a <- sapply(queries, method="string", n=1L, textcnt)
  b <- sapply(productid, getProductDescFromQuery)

  b
}

numbers <- function(searchTerms, titles){
  a<-mapply(regmatches,searchTerms,lapply(searchTerms,function(v){gregexpr("[0-9]+",v)}))
  b<-mapply(regmatches,titles,lapply(titles,function(v){gregexpr("[0-9]+",v)}))
  c<-mapply(vintersect,a,b)
  feature<- sapply((mapply(function(x,y){if(length(x)>0){length(x)==length(y)}else FALSE},a,c)), as.numeric)
  unname(feature)
}

test<-function(){
  summary(all.queryterms(queries$search_term, queries$product_title))
}

Selectdescriptions<-function(numbers,des){
  i<-1
  j<-1
  return<-list()
  while(i<=length(numbers))
  {
    if(numbers[i]==des[j,1])
    {
      return[[toString(des[j,1])]] <- des[j,2]
      i<-i+1
    }
    j<-j+1
  }
  return
}


ReadInfunc<- function(){
  tmpQueries<-read.csv(file="query_product.csv", row.names = 1, stringsAsFactors = FALSE)
  queries<<-tmpQueries[(tmpQueries$relevance)%%1==0,]
  
  a<-sort(unique(queries$product_uid, FALSE))
  tmpdescriptions<-read.csv(file="product_descriptions.csv", stringsAsFactors = FALSE)
  descriptions<<-Selectdescriptions(a,tmpdescriptions)
}


checkFunc<-function(){
  !exists("queries")||!exists("descriptions")
}
