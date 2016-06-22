library("NLP")
library("tau")
library("tm")
library("MASS")
library("nnet")
library("vecsets")
library("e1071") #heeft naive bayes functie kan handig zijn(?)
library("SnowballC")

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
  x<-length(searchTerms)*2/3
  testset<-queries[x+1:length(queries)]
  queries<-queries[1:x]
  searchTerms <- queries$search_term
  searchTermsDigits <- mapply(regmatches, searchTerms, lapply(searchTerms, function(v){gregexpr("[0-9]+", v)}))
  searchTermsNoDigits <- sapply(sapply(gsub("[[:punct:]]+", "",searchTerms), strsplit, "[[:space:][:punct:][:digit:]]+"),PluralToSingle)
  
  productTitles <- queries$product_title
  productTitlesDigits <- mapply(regmatches, productTitles, lapply(productTitles, function(v){gregexpr("[0-9]+", v)}))
  productTitlesNoDigits <- sapply(sapply(gsub("[[:punct:]]+", "",productTitles), strsplit, "[[:space:][:punct:][:digit:]]+"), PluralToSingle)
  
  ProductDescriptions <- sapply(queries$product_uid, getProductDescFromQuery)
  ProductDescriptionsDigits <- mapply(regmatches, ProductDescriptions, lapply(ProductDescriptions, function(v){gregexpr("[0-9]+", v)}))
  ProductDescriptionsNoDigits <- sapply(sapply(gsub("[[:punct:]]+", "",ProductDescriptions), strsplit, "[[:space:][:punct:][:digit:]]+"), PluralToSingle)
  
  print("start allterms")
  allterms <- all.queryterms(searchTermsNoDigits,productTitlesNoDigits)
  print("start alltermsdesc")
  alltermsdesc <- all.queryterms(searchTermsNoDigits, ProductDescriptionsNoDigits)
  print("start allnumbers")
  allnumbers <- numbers(searchTermsDigits, productTitlesDigits)
  print("start allnumbersdesc")
  allnumbersdesc <- numbers(searchTermsDigits, ProductDescriptionsDigits)
  print("start allorders")
  allorders <- orderfunc(searchTermsNoDigits,productTitlesNoDigits)
  print("start allordersdesc")
  allordersdesc <- orderfunc(searchTermsNoDigits, ProductDescriptionsNoDigits)
  print("start allabbreviations")
  allabbr <- abbreviationFunc(searchTermsNoDigits,productTitlesNoDigits)
  print("start allabbreviationsdesc")
  allabbrdesc <- abbreviationFunc(searchTermsNoDigits,ProductDescriptionsNoDigits)
  print("start alllengthsdesc")
  lengthsdesc<-checkwords(ProductDescriptions)
  print("start allreverseterms")
  allreverseterms <- all.reversequeryterms(searchTermsNoDigits, ProductDescriptionsNoDigits)
  frame <- data.frame(allterms, alltermsdesc, allnumbers, allnumbersdesc, allorders, allordersdesc, queries$relevance)
  m <<- polr(as.factor(queries.relevance) ~ allterms + alltermsdesc + allnumbers + allnumbersdesc + allorders + allordersdesc + allabbr + allabbrdesc + lengthsdesc + allreverseterms, data = frame, Hess=TRUE)
  
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
  c<-mapply(intersect,a,b)
  feature<- (mapply(function(x,y){if(length(x)>0 & length(y)>0){length(x)/length(y)}else {0}},c,a))
  unname(feature)
}

all.reversequeryterms <- function(queries, docs) {
  a<-sapply(sapply(queries, method="string",n=1L, textcnt), names)
  b<-sapply(sapply(docs, method="string",n=1L, textcnt), names)
  c<-mapply(intersect,a,b)
  feature<- (mapply(function(x,y){if(length(x)>0 & length(y)>0){length(x)/length(y)}else {0}},c,b))
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
#3 4 7 

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
  unname(mapply(orderamount, searchTerms, titles))
}
# gaat de volledige variant in
checkwords<-function(titles){
  unname(sapply(sapply(titles, strsplit, "[[:alpha:][:punct:][:digit:]]+"),length))
}
abbreviationcheck<-function(st,tl){
  abbcount<-1
  return<-0
  while(abbcount<=length(st)){
    if(nchar(st[abbcount])<5&nchar(st[abbcount])>1)
    {
      word<-1
      position<-1
      first<-substring(st[abbcount],1,1)
      now<-first
      while(word+nchar(st[abbcount])-position<=length(tl)){
        if(now==substring(tl[word],1,1)){
          position<-position+1
          now<-substr(st[abbcount],position,position)
          if(position>nchar(st[abbcount])){
            return<-1+return
            break
          }
        }
        else{
          if(first==substr(tl[word],1,1)){
            awposition<-2
            now<-substr(st[abbcount],2,2)
          }
          else{
            now<-first
            position<-1
          }
        }
        word<-word+1
      }
    }
    abbcount<-abbcount+1
  }
  return
}

#gaat de noDigitvariant in
abbreviationFunc<-function(searchTerms,titles){
  unname(mapply(abbreviationcheck, searchTerms, titles))
}

tfidf <- function(queries, descriptions) {
  corpus <- Corpus(VectorSource(descriptions))
  corpus <- tm_map(corpus, stripWhitespace)
  corpus <- tm_map(corpus, removePunctuation)
  corpus <- tm_map(corpus, removeWords, stopwords("english"))
  corpus <- tm_map(corpus, stemDocument, language="english")
  terms <-DocumentTermMatrix(corpus, control = list(weighting = function(x) weightTfIdf(x, normalize = FALSE)))
  #inspect(terms[1])
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
