library(ggplot2)
source("homebase.R")

plot_rotation <- function(outputfile, data) {
    tmp <- data[-(1:2),]
    p <- ggplot() + geom_point(mapping=aes(y=response, x=signedRotation, shape=f12), data=tmp) +
         labs(x="Target orientation (deg)", y="Response (deg)") +
         theme(legend.position="inside", legend.position.inside =c(0.1, 0.85))
    ggsave(filename=outputfile, plot=p)
}

args <- commandArgs(trailingOnly = TRUE)
if(length(args) != 2) {
    stop("Usage Rscript validate_backward.R file_to_be_validated outputfile.pdf")
}
filename = args[1]
output = args[2]

data <- validate_and_return_rotation(filename)
plot_rotation(output, data)
print(sprintf("%s plotted in %s", filename, output))

