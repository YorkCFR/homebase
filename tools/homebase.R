#
# This file provides a common definition of validation and loading tools
# for the various file formats that are created by the homebase application
#
# Some earlier versions of the softwaer capitalized column names slightly differently
# you will have to manually edit those column names if processing older files
#
# source('homebase.R') in your application to include these.
#
# Copyright (c) Michael Jenkin, 2026

validate_and_return_tracker <- function(filename) {
    if(!file.exists(filename)) {
        stop(sprintf("File %s does not exist", filename))
    }
    tryCatch({
        data <- read.csv(filename)
    }, error = function(e) {
        stop(sprintf("File %s did not parse error %s", filename, e$message))
    })

    expected <- c("Time", "cam.pos.x", "cam.pos.y", "cam.pos.z", "cam.rot.x", "cam.rot.y",
                 "cam.rot.z", "cam.rot.w", "head.pos.x", "head.pos.y", "head.pos.z", 
                 "head.rot.x", "head.rot.y", "head.rot.z", "head.rot.w")
    headers <- colnames(data)

    if(length(headers) != length(expected)) {
        stop(sprintf("Expected %d columns but got %d", length(expected), length(headers)))
    }
    for(i in 1:length(expected)) {
        if(headers[i] != expected[i]) {
            stop(sprintf("Column %d expected %s got %s", i, expected[i], headers[i]))
        }
    }
    if(nrow(data) < 1) {
        stop(sprintf("There were no rows!"))
    }
    return(data)
}

validate_and_return_backward <- function(filename) {
    if(!file.exists(filename)) {
        stop(sprintf("File %s does not exist", filename))
    }
    tryCatch({
        data <- read.csv(filename)
    }, error = function(e) {
        stop(sprintf("File %s did not parse error %s", filename, e$message))
    })

    expected <- c("cond", "starttime", "targetd", "pitch",
                 "spinDir", "inittarget", "finaltarget", "cam.pos.x",
                 "cam.pos.y", "cam.pos.z", "cam.rot.x", "cam.rot.y",
                 "cam.rot.z", "cam.rot.w", "reticle.pos.x", "reticle.pos.y",
                 "reticle.pos.z", "reticle.rot.x", "reticle.rot.y", "reticle.rot.z",
                 "reticle.rot.w")
    headers <- colnames(data)

    if(length(headers) != length(expected)) {
        stop(sprintf("Expected %d columns but got %d", length(expected), length(headers)))
    }
    for(i in 1:length(expected)) {
        if(headers[i] != expected[i]) {
            stop(sprintf("Column %d expected %s got %s", i, expected[i], headers[i]))
        }
    }
    if(nrow(data) != 6) {
        stop(sprintf("Should be 6 rows, but there are %d", nrow(data)))
    }

#   recode orientation information to make it easier to read

    names(data)[names(data) == "pitch"] <- "axis"
    data$axis <- trimws(as.character(data$axis))
    data$axis[data$axis == "True"] <- "pitch"
    data$axis[data$axis =="False"] <- "yaw"

    data$spinDir <- ifelse((data$axis == "pitch") & (data$spinDir == 1), "down", 
                           ifelse((data$axis == "pitch") & (data$spinDir == -1), "up", 
                           ifelse((data$axis == "yaw") & (data$spinDir == 1), "right",
                           ifelse((data$axis == "yaw") & (data$spinDir == -1), "left", "??"))))

    if((data[1,4] != "yaw") | (data[1,5] != "right")) {
        stop(sprintf("training row 1 not yaw/right %s/%s", data[1,4], data[1,5]))
    }
    if((data[2,4] != "pitch") | (data[2,5] != "up")) {
        stop(sprintf("training row 2 not pitch/up %s/%s", data[2,4], data[2,5]))
    }

    data$axis <- as.factor(data$axis)
    data$spinDir <- as.factor(data$spinDir)
    data$f12<-interaction(data$axis, data$spinDir)
    data$f12 <-factor(data$f12, levels = c("pitch.up", "pitch.down", "yaw.left", "yaw.right"))

    return(data)
}


validate_and_return_forward <- function(filename) {
    if(!file.exists(filename)) {
        stop(sprintf("File %s does not exist", filename))
    }
    tryCatch({
        data <- read.csv(filename)
    }, error = function(e) {
        stop(sprintf("File %s did not parse error %s", filename, e$message))
    })

    expected <- c( "cond", "starttime", "targetd", "rotation", "pitch", "spinDir", "inittarget", "finaltarget", "cam.pos.x", "cam.pos.y", "cam.pos.z", "cam.rot.x", "cam.rot.y", "cam.rot.z", "cam.rot.w", "reticle.pos.x", "reticle.pos.y", "reticle.pos.z", "reticle.rot.x", "reticle.rot.y", "reticle.rot.z", "reticle.rot.w")

    headers <- colnames(data)

    if(length(headers) != length(expected)) {
        stop(sprintf("Expected %d columns but got %d", length(expected), length(headers)))
    }
    for(i in 1:length(expected)) {
        if(headers[i] != expected[i]) {
            stop(sprintf("Column %d expected %s got %s", i, expected[i], headers[i]))
        }
    }
    if(nrow(data) != 18) {
        stop(sprintf("Should be 18 rows, but there are %d", nrow(data)))
    }

#   recode orientation information to make it easier to read

    names(data)[names(data) == "pitch"] <- "axis"
    data$axis <- trimws(as.character(data$axis))
    data$axis[data$axis == "True"] <- "pitch"
    data$axis[data$axis =="False"] <- "yaw"

    data$spinDir <- ifelse((data$axis == "pitch") & (data$spinDir == 1), "down", 
                           ifelse((data$axis == "pitch") & (data$spinDir == -1), "up", 
                           ifelse((data$axis == "yaw") & (data$spinDir == 1), "right",
                           ifelse((data$axis == "yaw") & (data$spinDir == -1), "left", "??"))))

    if((data[1,5] != "yaw") | (data[1,6] != "left")) {
        stop(sprintf("training row 1 not yaw/right %s/%s", data[1,5], data[1,6]))
    }
    if((data[2,5] != "pitch") | (data[2,6] != "up")) {
        stop(sprintf("training row 2 not pitch/up %s/%s", data[2,5], data[2,6]))
    }

    data$targetd <- as.factor(data$targetd)
    data$axis <- as.factor(data$axis)
    data$spinDir <- as.factor(data$spinDir)
    data$f12<-interaction(data$axis, data$spinDir)
    data$f12 <-factor(data$f12, levels = c("pitch.up", "pitch.down", "yaw.left", "yaw.right"))

    return(data)
}


validate_and_return_rotation <- function(filename) {
    if(!file.exists(filename)) {
        stop(sprintf("File %s does not exist", filename))
    }
    tryCatch({
        data <- read.csv(filename)
    }, error = function(e) {
        stop(sprintf("File %s did not parse error %s", filename, e$message))
    })

    expected <- c( "cond", "starttime", "rotation", "pitch", "spinDir", "response", "cam.pos.x", "cam.pos.y", "cam.pos.z", "cam.rot.x", "cam.rot.y", "cam.rot.z", "cam.rot.w", "reticle.pos.x", "reticle.pos.y", "reticle.pos.z", "reticle.rot.x", "reticle.rot.y", "reticle.rot.z", "reticle.rot.w")

    headers <- colnames(data)

    if(length(headers) != length(expected)) {
        stop(sprintf("Expected %d columns but got %d", length(expected), length(headers)))
    }
    for(i in 1:length(expected)) {
        if(headers[i] != expected[i]) {
            stop(sprintf("Column %d expected %s got %s", i, expected[i], headers[i]))
        }
    }
    if(nrow(data) != 26) {
        stop(sprintf("Should be 26 rows, but there are %d", nrow(data)))
    }

#   recode orientation information to make it easier to read

    names(data)[names(data) == "pitch"] <- "axis"
    names(data)[names(data) == "spindir"] <- "spinDir"  # make name of spinDir consistent. Note, if fixed in code this will have no effect
    data$axis <- trimws(as.character(data$axis))
    data$axis[data$axis == "True"] <- "pitch"
    data$axis[data$axis =="False"] <- "yaw"
    data$spinDir <- as.integer(data$spinDir)
    data$signedRotation <- (180-data$rotation) * data$spinDir


    data$spinDir <- ifelse((data$axis == "pitch") & (data$spinDir == 1), "down", 
                           ifelse((data$axis == "pitch") & (data$spinDir == -1), "up", 
                           ifelse((data$axis == "yaw") & (data$spinDir == 1), "right",
                           ifelse((data$axis == "yaw") & (data$spinDir == -1), "left", "??"))))

    if((data[1,4] != "pitch") | (data[1,5] != "up")) {
        stop(sprintf("training row 1 not pitch/up %s/%s", data[1,4], data[1,5]))
    }
    if((data[2,4] != "yaw") | (data[2,5] != "right")) {
        stop(sprintf("training row 2 not yaw/right %s/%s", data[2,4], data[2,5]))
    }

    data$rotation <- as.factor(data$rotation)
    data$axis <- as.factor(data$axis)
    data$spinDir <- as.factor(data$spinDir)
    data$f12<-interaction(data$axis, data$spinDir)
    data$f12 <-factor(data$f12, levels = c("pitch.up", "pitch.down", "yaw.left", "yaw.right"))

    return(data)
}

validate_and_return_triangle <- function(filename) {
    if(!file.exists(filename)) {
        stop(sprintf("File %s does not exist", filename))
    }
    tryCatch({
        data <- read.csv(filename)
    }, error = function(e) {
        stop(sprintf("File %s did not parse error %s", filename, e$message))
    })

    expected <- c("cond", "backTime", "len1", "angle", "pitch", "spinDir", "len2", "dirInit", "dirFinal", "angleFinal", "cam.pos.x", "cam.pos.y", "cam.pos.z", "cam.rot.x", "cam.rot.y", "cam.rot.z", "cam.rot.w", "reticle.pos.x", "reticle.pos.y", "reticle.pos.z", "reticle.rot.x", "reticle.rot.y", "reticle.rot.z", "reticle.rot.w")


    headers <- colnames(data)

    if(length(headers) != length(expected)) {
        stop(sprintf("Expected %d columns but got %d", length(expected), length(headers)))
    }
    for(i in 1:length(expected)) {
        if(headers[i] != expected[i]) {
            stop(sprintf("Column %d expected %s got %s", i, expected[i], headers[i]))
        }
    }
    if(nrow(data) != 50) {
        stop(sprintf("Should be 50 rows, but there are %d", nrow(data)))
    }

#   recode orientation information to make it easier to read

    names(data)[names(data) == "pitch"] <- "axis"
    names(data)[names(data) == "anglefinal"] <- "angleFinal"  # make name of spinDir consistent. Note, if fixed in code this will have no effect
    names(data)[names(data) == "dirfinal"] <- "dirFinal"  # make name of spinDir consistent. Note, if fixed in code this will have no effect
    data$axis <- trimws(as.character(data$axis))
    data$axis[data$axis == "True"] <- "pitch"
    data$axis[data$axis =="False"] <- "yaw"
    data$spinDir <- as.integer(data$spinDir)
    data$signedAngle <- (180-data$angle) * data$spinDir


    data$spinDir <- ifelse((data$axis == "pitch") & (data$spinDir == 1), "down", 
                           ifelse((data$axis == "pitch") & (data$spinDir == -1), "up", 
                           ifelse((data$axis == "yaw") & (data$spinDir == 1), "right",
                           ifelse((data$axis == "yaw") & (data$spinDir == -1), "left", "??"))))

    if((data[1,5] != "yaw") | (data[1,6] != "right")) {
        stop(sprintf("training row 1 not yaw/right %s/%s", data[1,5], data[1,6]))
    }
    if((data[2,5] != "pitch") | (data[2,6] != "down")) {
        stop(sprintf("training row 2 not pitch/down %s/%s", data[2,5], data[2,6]))
    }

    data$angle <- as.factor(data$angle)
    data$signedAngle <- data$signedAngle
    data$axis <- as.factor(data$axis)
    data$spinDir <- as.factor(data$spinDir)
    data$f12<-interaction(data$axis, data$spinDir)
    data$f12 <-factor(data$f12, levels = c("pitch.up", "pitch.down", "yaw.left", "yaw.right"))

    return(data)
}



get_all_backward <- function(dir) {
    all <- list.dirs(path=dir)
    all_data <- data.frame()
    for(d in all) {
        if(d != ".") {
            subj <- substring(d, 3)  				# trim the ./ on each string
            t <- file.path(d, "Responses_linear_backward*.txt") # only one per participant directory, right?
            path <- Sys.glob(t)
            data <- validate_and_return_backward(path)
            data <- data[-(1:2),]                               # trim practice runs
            data$subjid <- subj
            all_data <- rbind(all_data, data)
        }
    }
    return(all_data)
}


get_all_rotation <- function(dir) {
    all <- list.dirs(path=dir)
    all_data <- data.frame()
    for(d in all) {
        if(d != ".") {
            subj <- substring(d, 3)  				# trim the ./ on each string
            t <- file.path(d, "Responses_rotation*.txt") # only one per participant directory, right?
            path <- Sys.glob(t)
            data <- validate_and_return_rotation(path)
            data <- data[-(1:2),]                               # trim practice runs
            data$subjid <- subj
            all_data <- rbind(all_data, data)
        }
    }
    return(all_data)
}

