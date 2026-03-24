library(ggplot2)
source("homebase.R")




plot_all_backward <- function(outputfile, data) {
    p <- ggplot(mapping=aes(y=finaltarget, x=f12), data=data) +
         geom_violin(trim=FALSE, fill='#A4A4A4', color='black') +
         geom_boxplot(width=0.1) +
#         geom_dotplot(binaxis='y', stackdir='center', dotsize=5) +
         labs(x="Condition", y="Response (target is at 8m)") +
         scale_x_discrete(labels=c("pitch.up"="Pitch Up", "pitch.down" ="Pitch Down", "yaw.left"="Yaw Left", "yaw.right" ="Yaw Right")) +
         coord_cartesian(ylim=c(0, 16))
    ggsave(filename=outputfile, plot=p)
}

args <- commandArgs(trailingOnly = TRUE)
if(length(args) != 2) {
    stop("Usage: Rscript summarize_backward.R data_directory outputfile.pdf")
}
data_directory = args[1]
output = args[2]

data <- get_all_backward(data_directory)
print(data)
plot_all_backward(output, data)
print(sprintf("Violin plot of %s  can be found in %s", data_directory, output))

