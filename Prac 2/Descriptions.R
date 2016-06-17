

BinarySearch <- function(value, list, min, max) {
  middle <- (min+max)/2
  compareValue <- list[middle]
  if (value == compareValue) {
    return <- middle
  }
  else if (min == max) {
    return <- NULL
  }
  else if (value > compareValue) {
    return <- BinarySearch(value, list, middle+1, max)
  }
  else {
    return <- BinarySearch(value, list, min, middle)
  }
  return
}