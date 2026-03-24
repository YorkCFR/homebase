library(ggplot2)
library(dplyr)

source("homebase.R")

#
# Where the person pointed to
#
compute_points <- function(data) {
    tmp <- data[-(1:2),]
    all_data <- data.frame(x=numeric(), y=numeric(), r=numeric(), cond=character())

    for(i in 1:nrow(tmp)) {
        row <- tmp[i, ]

        a <- 0.0
        b <- 0.0
        c <-  a + 0.0
        d <-  b - row$len1
        e <- c + row$len2 * sin(pi * row$signedAngle / 180)
        f <- d + row$len2 * cos(pi * row$signedAngle / 180)
        g <- e + row$dirFinal * sin(pi * (row$angleFinal + row$signedAngle) / 180)
        h <- f + row$dirFinal * cos(pi * (row$angleFinal + row$signedAngle) / 180)
        r <- sqrt(g*g + h * h)

        data <- c(g, h, r, as.character(row$axis))
        all_data <- rbind(all_data, data)
    }
    colnames(all_data) <- c("x", "y", "r", "axis")
    all_data$x <- as.numeric(all_data$x)
    all_data$y <- as.numeric(all_data$y)
    all_data$r <- as.numeric(all_data$r)
    all_data$axis <- as.factor(all_data$axis)
    return(all_data)
}

args <- commandArgs(trailingOnly = TRUE)
if(length(args) != 2) {
    stop("Usage Rscript validate_backward.R file_to_be_validated outputfile.pdf")
}
filename = args[1]
output = args[2]

data <- validate_and_return_triangle(filename)
print("centres")
centres <- compute_points(data)
print(centres)

data_with_stats <- centres %>%
                   group_by(axis) %>%
                   mutate(
                       group_mean_x = mean(x),
                       group_mean_y = mean(y),
                       group_mean_r = mean(r),
                       group_sd_x = sd(x),
                       group_sd_y = sd(y),
                       group_sd_r = sd(r)
                   ) %>% ungroup()
options(width=2000)
print(data_with_stats)
data_with_stats <- centres %>%
                   group_by(axis) %>%
                   mutate(
                       close = ifelse(r > group_mean_r * 2, 0, 1)
                   ) %>% ungroup()
print(data_with_stats$close)




print(sprintf("%s plotted as %s", filename, output))

