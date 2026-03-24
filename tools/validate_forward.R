library(ggplot2)
source("homebase.R")


plot_forward <- function(outputfile, data) {
    tmp <- data[-(1:2),]
    p <- ggplot() + geom_point(mapping=aes(y=finaltarget, x=targetd, shape=f12), data=tmp) +
         labs(x="Target distance (m)", y="Response (m)") +
         coord_cartesian(ylim=c(0, 16)) +
#         theme_classic() +
         theme(legend.position="inside", legend.position.inside =c(0.9, 0.85))
    ggsave(filename=outputfile, plot=p)
}

args <- commandArgs(trailingOnly = TRUE)
if(length(args) != 2) {
    stop("Usage Rscript validate_backward.R file_to_be_validated outputfile.pdf")
}
filename = args[1]
output = args[2]

data <- validate_and_return_forward(filename)
plot_forward(output, data)
print(sprintf("%s output in %s", filename, output))

