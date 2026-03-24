library(ggplot2)
library(dplyr)

source("homebase.R")



plot_all_rotation <- function(outputfile, data) {
    p <- ggplot(mapping=aes(y=response, x=signedRotation), data=data) +
         geom_boxplot(width=0.1) +
#         geom_dotplot(binaxis='y', stackdir='center', dotsize=5) +
         labs(x="Condition", y="Response (target is at 8m)") +
         scale_x_discrete(labels=c("pitch.up"="Pitch Up", "pitch.down" ="Pitch Down", "yaw.left"="Yaw Left", "yaw.right" ="Yaw Right")) +
         coord_cartesian(ylim=c(-90, 90))
    ggsave(filename=outputfile, plot=p)
}

args <- commandArgs(trailingOnly = TRUE)
if(length(args) != 2) {
    stop("Usage: Rscript summarize_rotation.R data_directory outputfile.pdf")
}
data_directory = args[1]
output = args[2]

data <- get_all_rotation(data_directory)





yaw_data <- subset(data, axis == "yaw")

yaw_summary <- yaw_data %>%
           group_by(signedRotation) %>%
           summarize(
               mean_response = mean(response),
               sd_response = sd(response)
           )
yaw_summary$axis <- "yaw"
            
pitch_data <- subset(data, axis == "pitch")
pitch_summary <- pitch_data %>%
           group_by(signedRotation) %>%
           summarize(
               mean_response = mean(response),
               sd_response = sd(response)
           )
pitch_summary$axis <- "pitch"

all_summary <- rbind(yaw_summary, pitch_summary)
all_summary$axis <- as.factor(all_summary$axis)
all_summary$signedRotation <- as.numeric(all_summary$signedRotation)
print(all_summary)

p <- ggplot(mapping=aes(y=mean_response, x=signedRotation, shape=axis), data=all_summary) +
          geom_point() +
         geom_errorbar(aes(ymin=mean_response-sd_response, ymax=mean_response+sd_response), width=0.2) +
         geom_smooth(method="lm", se=FALSE, formula = y ~ x + 0, color="black") +
         scale_linetype_manual(values=c("yaw"="solid", "pitch"="dashed"))
         labs(x = "Signed rotation (deg)", y="Signed response (deg)")
ggsave(filename=output, plot=p)
            

#plot_all_rotation(output, data)
print(sprintf("plot of %s  can be found in %s", data_directory, output))

