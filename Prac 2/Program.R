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
#lengte meter door spaties te tellen
Main<- function(){
  #start het programma
  #readQueryProduct()
  
  
  print("start reading")
  #head(description)
  if(checkFunc())
    ReadInfunc()
  
  searchTerms <- queries$search_term
  searchTermsNoDigits <- mapply(regmatches, searchTerms, lapply(searchTerms, function(v){gregexpr("[0-9]+", v)}))
  searchTerms <- sapply(searchTermsNoDigits, strsplit, "[[:space:][:punct:][:digit:]]+")
  
  descriptionList = sapply(queries$product_uid, getProductDescFromQuery)
  
  print("start allterms")
  allterms <- all.queryterms(searchTerms,queries$product_title)
  print("start alltermsdesc")
  alltermsdesc <- all.queryterms(searchTerms, descriptionList)
  print("start allnumbers")
  allnumbers <- numbers(searchTerms, queries$product_title)
  print("start allnumbersdesc")
  allnumbersdesc <- numbers(searchTerms, descriptionList)
  print("start allorders")
  allorders <- orderfunc(searchTerms,queries$product_title)
  print("start allordersdesc")
  allordersdesc <- orderfunc(searchTerms, descriptionList)
  
  frame <- data.frame(allterms, alltermsdesc, allnumbers, allnumbersdesc, allorders, allordersdesc, queries$relevance)
  m <<- polr(as.factor(queries.relevance) ~ allterms + alltermsdesc + allnumbers + allnumbersdesc + allorders + allordersdesc, data = frame, Hess=TRUE)
  
  #werkelijk geen idee wat ik hier doe maar dit komt uit de slides
  #zit ook nog te denken hoe we dus gaan gokken
  #qp.dat<-data.frame(relevance=queries$relevance,allterms=allterms)
  #tr.index<-sample(length(queries$search_term),length(queries)*2/3)
  #qp.lm<-lm(relevance~allterms,data=qp.dat[tr.index,])
  #summary(qp.lm)
  
  summary(m)
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
PluralToSingle<-function(x){
  xstring<-x
  if(nchar(xstring)>3){
    if(substr(xstring,nchar(xstring)-2,nchar(xstring))=="ies"){
      xstring<-paste0(substr(xstring,1,nchar(xstring)-3),"y")
    }
    else
      if(substr(xstring,nchar(xstring),nchar(xstring))=="s")
      {
        xstring<-substr(xstring,1,nchar(xstring)-1)
        if(substr(xstring,nchar(xstring),nchar(xstring))=="e")
        {
          xstring<-substring(xstring,1,nchar(xstring)-1)
          if(substr(xstring,nchar(xstring),nchar(xstring))=="v")
          {
            xstring<-paste0(substring(xstring,1,nchar(xstring)-1),"f")
          }
        }
      }
  }
  tolower(xstring)
}

#kijken of alle woorden voorkomen in de titel(en ook geen anderen heeft)
all.queryterms <- function (queries, docs)
{
  a<-sapply(sapply(queries, method="string",n=1L, textcnt), names)
  b<-sapply(sapply(docs, method="string",n=1L, textcnt), names)
  c<-mapply(intersect,sapply(a,names),b)
  feature<- (mapply(function(x,y){if(length(x)>0 & length(y)>0){length(x)/length(y)}else {0}},c,a))
  unname(feature)
}

getProductDescFromQuery <- function(id) {
  stringId <- toString(id)
  return <- unname(descriptions[stringId])[[1]]
  return
}

numbers <- function(searchTerms, titles){
  c<-mapply(vintersect,searchTerms,titles)
  feature<- (mapply(function(x,y){if(length(x)>0 & length(y)>0){length(x)/length(y)} else {0}},c,searchTerms))
  unname(feature)
}


Selectdescriptions<-function(numbers,des){
  i<-1
  j<-1
  return <- list()
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

orderamount<-function(st,tl){
  l<-length(st)
  l2<-length(tl)
  i<-0
  j<-1
  c<-1
  while(j<=l){
    if(!(st[j] %in% tl)){
      i<-i+1
      }
    j<-1+j
    }
  max<-0
  matches<-match(tl,st)
  while((i<l) &(c<=l2))
  {
    if(!is.na(matches[c]))
    {
      if(max<(l-matches[c]-i+1))
      {
        max<-(l-matches[c]-i+1)
      }
      i<-i+1
    }
    c<-c+1
  }
  max
}
orderfunc<-function(searchTerms, titles){
  unname(mapply(orderamount, searchTerms ,titles))
}


ReadInfunc<- function(){
  tmpQueries<-read.csv(file="query_product.csv", stringsAsFactors = FALSE)
  queries<<-tmpQueries[(tmpQueries$relevance)%%1==0,]
  
  a<-sort(unique(queries$product_uid, FALSE))
  tmpdescriptions<-read.csv(file="product_descriptions.csv", stringsAsFactors = FALSE)
  descriptions<<-Selectdescriptions(a,tmpdescriptions)
  print("done reading")
}


checkFunc<-function(){
  !exists("queries")||!exists("descriptions")
}
