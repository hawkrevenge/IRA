library("NLP")
library("tau")
library("tm")
library("MASS")
library("nnet")
library("vecsets")
library("e1071") #heeft naive bayes functie kan handig zijn(?)
library("SnowballC")
library("hydroGOF")

#lengte meter door spaties te tellen
Main<- function(){
  print("start reading")
  if(checkFunc())
    ReadInfunc()
  x<-length(queries$id)*2/3
  
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
  
  frame <- data.frame(allterms, alltermsdesc, allnumbers, allnumbersdesc, allorders, allordersdesc, allabbr, allabbrdesc, lengthsdesc, allreverseterms, queries$relevance)
  
  testset<<-frame[((x+1):(length(queries$id))),]
  frame2 <<-frame[(1:x),]
  
  m0 <<- lm(queries.relevance ~ allterms + alltermsdesc + allnumbers + allnumbersdesc + allorders + allordersdesc + allabbr + allabbrdesc + lengthsdesc + allreverseterms, data = frame2)
  m1 <<- polr(as.factor(queries.relevance) ~ allterms + alltermsdesc + allnumbers + allnumbersdesc + allorders + allordersdesc + allabbr + allabbrdesc + lengthsdesc + allreverseterms, data = frame2)
  m2 <<- multinom(as.factor(queries.relevance) ~ allterms + alltermsdesc + allnumbers + allnumbersdesc + allorders + allordersdesc + allabbr + allabbrdesc + lengthsdesc + allreverseterms, data = frame2)
  
  newm0 <<- lm(formula = queries.relevance ~ allterms + alltermsdesc + allorders +
                 allreverseterms + allabbrdesc + allnumbers + allnumbersdesc +
                 allordersdesc + allterms:allorders + allterms:alltermsdesc +
                 alltermsdesc:allreverseterms + allnumbers:allnumbersdesc +
                 allorders:allreverseterms + allterms:allreverseterms + allreverseterms:allnumbers +
                 alltermsdesc:allorders + allabbrdesc:allnumbers + allorders:allordersdesc +
                 alltermsdesc:allordersdesc + allabbrdesc:allordersdesc +
                 allnumbers:allordersdesc + alltermsdesc:allnumbers + alltermsdesc:allnumbersdesc +
                 allreverseterms:allnumbersdesc + allterms:allordersdesc +
                 allreverseterms:allabbrdesc, data = frame2)
  
  newm1 <<- polr(formula = as.factor(queries.relevance) ~ allterms + alltermsdesc +
                   allreverseterms + allorders + allabbrdesc + allnumbers +
                   allnumbersdesc + lengthsdesc + allordersdesc + alltermsdesc:allreverseterms +
                   allterms:allorders + allterms:alltermsdesc + allnumbers:allnumbersdesc +
                   allreverseterms:lengthsdesc + alltermsdesc:allordersdesc +
                   allorders:allordersdesc + alltermsdesc:allnumbers + alltermsdesc:allorders +
                   alltermsdesc:allnumbersdesc + allabbrdesc:allordersdesc +
                   allnumbers:lengthsdesc + allreverseterms:allnumbersdesc +
                   allreverseterms:allorders + allorders:allnumbersdesc + allreverseterms:allabbrdesc +
                   allterms:allnumbers, data = frame2)
  
  
  newm2 <<- multinom(formula = as.factor(queries.relevance) ~ allterms + 
                           alltermsdesc + allreverseterms + allorders + lengthsdesc + 
                           allnumbers + allnumbersdesc + allordersdesc + allabbrdesc + 
                           alltermsdesc:allreverseterms + allterms:allorders + allreverseterms:lengthsdesc + 
                           allterms:alltermsdesc + allnumbers:allnumbersdesc + alltermsdesc:allorders + 
                           allreverseterms:allorders + allreverseterms:allnumbers + 
                           lengthsdesc:allnumbers + alltermsdesc:allordersdesc + allnumbersdesc:allordersdesc + 
                           allorders:allordersdesc + allterms:allnumbers + alltermsdesc:lengthsdesc + 
                           allordersdesc:allabbrdesc + allorders:allnumbers + allterms:allreverseterms + 
                           allterms:allordersdesc + allnumbersdesc:allabbrdesc + allorders:allabbrdesc, 
                         data = frame2)
  
  nullm0 <<- lm(queries.relevance ~ 1, data=frame2)
  nullm1 <<- polr(as.factor(queries.relevance) ~ 1, data=frame2)
  nullm2 <<- multinom(as.factor(queries.relevance) ~ 1, data=frame2)
  
  pred0 <<- predict(newm0, testset)
  pred1 <<- predict(newm1, testset)
  pred2 <<- predict(newm2, testset)
  
  #step(nullm0, scope=list(lower=nullm0, upper=m0), direction="forward")
  
  #print(summary(m0))
  #print(summary(m1))
  #print(summary(m2))
  #print(table(pred1,testset$queries.relevance))
  #print(table(pred2,testset$queries.relevance))
  #confusion matrix for lm does not work, as it guesses linear values (e.g 2.14) which will not coincide with real relevance values (1, 2 or 3)
}

readQueryProduct <- function() {
  query_product.dat <- read.csv("query_product.csv", stringsAsFactors=FALSE)
  str(query_product.dat)
  query_product.dat[query_product.dat[,1] == 9]
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
            xstring<-paste0(substring(xstring,1,nchar(xstring)-1),"fe")
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

ReadInfunc<- function(){
  tmpQueries<-read.csv(file="query_product.csv", stringsAsFactors = FALSE)
  queries<<-tmpQueries[(tmpQueries$relevance)%%1==0,]
  #queries <<-tmpQueries
  
  a<-sort(unique(queries$product_uid, FALSE))
  tmpdescriptions<-read.csv(file="product_descriptions.csv", stringsAsFactors = FALSE)
  descriptions<<-Selectdescriptions(a,tmpdescriptions)
  print("done reading")
}


checkFunc<-function(){
  !exists("queries")||!exists("descriptions")
}
