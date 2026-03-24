library(ggplot2)
source("homebase.R")


plot_backward <- function(outputfile, data) {
    tmp <- data[-(1:2),]
    p <- ggplot() + geom_point(mapping=aes(y=finaltarget, x=f12), data=tmp) +
         labs(x="Condition", y="Response (target is at 8)") +
         coord_cartesian(ylim=c(0, 16))
    ggsave(filename=outputfile, plot=p)
}

args <- commandArgs(trailingOnly = TRUE)
if(length(args) != 2) {
    stop("Usage: Rscript validate_backward.R file_to_be_validated outputfile.pdf")
}
filename = args[1]
output = args[2]

data <- validate_and_return_backward(filename)
plot_backward(output, data)
print(sprintf("Plot of %s  can be found in %s", filename, output))

