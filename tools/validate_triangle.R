library(ggplot2)
source("homebase.R")

plot_triangle <- function(outputfile, data) {
    tmp <- data[-(1:2),]

    plotme <- data.frame(x1=numeric(), y1=numeric(), x2=numeric(), y2=numeric())
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
        if(row$axis == "yaw") {
           colour <- "red";
        } else {
           colour <- "green";
        }

        nrow <- list(a, b, c, d, colour)
        plotme <- rbind(plotme, nrow)
        nrow <- list(c, d, e, f, colour)
        plotme <- rbind(plotme, nrow)
        nrow <- list(e, f, g, h, colour)
        plotme <- rbind(plotme, nrow)
    }
    names(plotme)[1] <- "x1"
    names(plotme)[2] <- "y1"
    names(plotme)[3] <- "x2"
    names(plotme)[4] <- "y2"
    names(plotme)[5] <- "colour"
    p <- ggplot() + geom_segment(mapping=aes(x = x1, y = y1, xend = x2, yend = y2, colour=colour), data=plotme) +
         xlab("Axis One") + ylab("Axis Two") +
         scale_color_manual(
             name = "Orientation",
             values = c("red", "green"),
             labels = c("Yaw", "Pitch"))
    ggsave(filename=outputfile, plot=p)
}

args <- commandArgs(trailingOnly = TRUE)
if(length(args) != 2) {
    stop("Usage Rscript validate_backward.R file_to_be_validated outputfile.pdf")
}
filename = args[1]
output = args[2]

data <- validate_and_return_triangle(filename)
plot_triangle(output, data)
print(sprintf("%s plotted as %s", filename, output))

