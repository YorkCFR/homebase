library(ggplot2)
source("homebase.R")

get_one_instance <- function(dir, pattern) {
    all <- list.files(path=dir)
    match <- startsWith(all, pattern)
    if(sum(match) != 1) {
        stop(print(sprintf("Expected one %s got %d", pattern, sum(match))))
    }
    pos <- match(TRUE, match)
    return(all[pos])
}

extract_time <- function(fname, head) {
    time <-substring(fname, nchar(head) + 1)
    time <- substring(time, 1, nchar(time)-4)
    return(time)
}

process_forward <- function(dir) {
    forward_file <- get_one_instance(directory, "Responses_linear_forward_")
    data <- validate_and_return_forward(paste(dir, "/", forward_file, sep=""))
    time <- extract_time(forward_file, "Resposnes_linear_forward_")
    print(sprintf("validated forward data file %s", forward_file))

    # process tracker data. There should be  18
    time <- extract_time(forward_file, "Resposnes_linear_forward_")
    for(i in 0:17) {
        loop_name <- paste(dir, "/HeadTracking_linear_forward_", time, "_", i, ".txt", sep="")
        tracker_data <- validate_and_return_tracker(loop_name)
        print(sprintf("    validate forward data tracking file %s", loop_name))
    }
}

process_backward <- function(dir) {
    backward_file <- get_one_instance(directory, "Responses_linear_backward_")
    data <- validate_and_return_backward(paste(dir, "/", backward_file, sep=""))
    time <- extract_time(backward_file, "Resposnes_linear_backward_")
    print(sprintf("validated backward data file %s", backward_file))

    # process tracker data. There should be  6
    time <- extract_time(backward_file, "Resposnes_linear_backward_")
    for(i in 0:5) {
        loop_name <- paste(dir, "/HeadTracking_linear_backward_", time, "_", i, ".txt", sep="")
        tracker_data <- validate_and_return_tracker(loop_name)
        print(sprintf("    validate backward data tracking file %s", loop_name))
    }
}

process_rotation <- function(dir) {
    rotation_file <- get_one_instance(directory, "Responses_rotation_")
    data <- validate_and_return_rotation(paste(dir, "/", rotation_file, sep=""))
    time <- extract_time(rotation_file, "Resposnes_rotation_")
    print(sprintf("validated rotation data file %s", rotation_file))

    # process tracker data. There should be  26
    time <- extract_time(rotation_file, "Resposnes_rotation_")
    for(i in 0:25) {
        loop_name <- paste(dir, "/HeadTracking_rotation_", time, "_", i, ".txt", sep="")
        tracker_data <- validate_and_return_tracker(loop_name)
        print(sprintf("    validate rotation data tracking file %s", loop_name))
    }
}


process_triangle <- function(dir) {
    triangle_file <- get_one_instance(directory, "Responses_triangle_")
    data <- validate_and_return_triangle(paste(dir, "/", triangle_file, sep=""))
    time <- extract_time(triangle_file, "Resposnes_triangle_")
    print(sprintf("validated triangle completion data file %s", triangle_file))

    # process tracker data. There should be 50
    time <- extract_time(triangle_file, "Resposnes_triangle_")
    for(i in 0:49) {
        loop_name <- paste(dir, "/HeadTracking_triangle_completion_", time, "_", i, ".txt", sep="")
        tracker_data <- validate_and_return_tracker(loop_name)
        print(sprintf("    validate triangle data tracking file %s", loop_name))
    }
}

args <- commandArgs(trailingOnly = TRUE)
print("validate_homebase.R - validate all the data from one data run")
if(length(args) != 1) {
    stop("Usage Rscript validate_homebase.R directory-containing-one-participants-data")
}
directory <- args[1]

print(sprintf("Validating data found in %s", directory))
print("Validating forward dataset")
process_forward(directory)
print("Forward dataset validated")
print("Validating backward dataset")
process_backward(directory)
print("Backward dataset validated")
print("Validating rotation dataset")
process_rotation(directory)
print("Rotation dataset validated")
print("Validating triangle completion dataset")
process_triangle(directory)
print("Triangle completion dataset validated")
print("Full dataset validated")
