csv.files = Sys.glob("csv/*.csv")

# Global containers
all.times = c()
all.attempts = c()
all.times.conventional = c()
all.times.kinetics = c()
all.attempts.conventional = c()
all.attempts.kinetics = c()

# Total number of attack tests (per challenge)
n.attacks = 12

# T-test containers
t.all.times = c()
t.all.attempts = c()

for (file in csv.files) {
  data = read.csv(file)
  # Filter out the practice data and lines with failed attempts
  data.ch.su = subset(data, TargetGestureID >= 0 & Success == 1)
  
  # Append to 'all' vectors
  all.times = c(all.times, data.ch.su$TimeSpent)
  all.attempts = c(all.attempts, as.numeric(data.ch.su$Attempts))

  # Filter to joystick-specific data frames
  data.ch.su.kinetics = subset(data.ch.su, IsConventional == 0)
  data.ch.su.conventional = subset(data.ch.su, IsConventional == 1)
  
  attach(data.ch.su.conventional)
  # Append to 'all' vectors
  all.times.conventional = c(all.times.conventional, TimeSpent)
  all.attempts.conventional = c(all.attempts.conventional, as.numeric(Attempts))
  # Extract the relevant columns
  times.conventional = TimeSpent
  attempts.conventional = as.numeric(Attempts)
  detach(data.ch.su.conventional)

  attach(data.ch.su.kinetics)
  # Append to 'all' vectors
  all.times.kinetics = c(all.times.kinetics, TimeSpent)
  all.attempts.kinetics = c(all.attempts.kinetics, as.numeric(Attempts))
  # Extract the relevant columns
  times.kinetics = TimeSpent
  attempts.kinetics = as.numeric(Attempts)
  detach(data.ch.su.kinetics)

  # t-test the TimeSpent column
  meandiff.times = mean(times.kinetics - times.conventional)
  # the standard deviation is the square root of the variance
  sdev.times = sqrt(var(times.kinetics - times.conventional))
  # paired t-test equation (Grandin2003 p. 26)
  t.times = meandiff.times / (sdev.times / sqrt(n.attacks))
  
  # t-test the Attempts column
  meandiff.attempts = mean(attempts.kinetics - attempts.conventional)
  sdev.attempts = sqrt(var(attempts.kinetics - attempts.conventional))
  t.attempts = meandiff.attempts / (sdev.attempts / sqrt(n.attacks))

  # Append to 'all' vectors
  t.all.times = c(t.all.times, t.times)
  t.all.attempts = c(t.all.attempts, t.attempts)
}

# Calculate global mean values
mean.times.all = mean(all.times)
mean.attempts.all = mean(all.attempts)
mean.times.conventional = mean(all.times.conventional)
mean.attempts.conventional = mean(all.attempts.conventional)
mean.times.kinetics = mean(all.times.kinetics)
mean.attempts.kinetics = mean(all.attempts.kinetics)

