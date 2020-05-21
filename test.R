csv.files = Sys.glob("csv/*.csv")

# Global containers
all.times = c()
all.times.conventional = c()
all.times.kinetics = c()
all.attempts = c()
all.attempts.conventional = c()
all.attempts.kinetics = c()
all.distance = c()
all.distance.conventional = c()
all.distance.kinetics = c()
all.area = c()
all.area.conventional = c()
all.area.kinetics = c()

# Total number of attack tests (per challenge)
n.attacks = 12

# T-test containers
t.all.times = c()
t.all.attempts = c()
t.all.distance = c()
t.all.area = c()

# Paired t-test according to Grandin2003 p. 26
ttest = function(x, y, n) {
  meandiff = mean(x - y)
  sdev = sqrt(var(x - y))
  if (sdev == 0) {
    t = 0
  } else {
    t = meandiff / (sdev / sqrt(n))
  }
  t
}

for (file in csv.files) {
  data = read.csv(file)
  # Filter out the practice data and lines with failed attempts
  data.ch.su = subset(data, TargetGestureID >= 0 & Success == 1)
  
  # Append to 'all' vectors
  all.times = c(all.times, data.ch.su$TimeSpent)
  all.attempts = c(all.attempts, as.numeric(data.ch.su$Attempts))
  all.distance = c(all.distance, data.ch.su$TotalDistance)
  all.area = c(all.area, data.ch.su$TotalArea)

  # Filter to joystick-specific data frames
  data.ch.su.kinetics = subset(data.ch.su, IsConventional == 0)
  data.ch.su.conventional = subset(data.ch.su, IsConventional == 1)
  
  attach(data.ch.su.conventional)
  # Extract the relevant columns
  times.conventional = TimeSpent
  attempts.conventional = as.numeric(Attempts)
  distance.conventional = TotalDistance
  area.conventional = TotalArea
  detach(data.ch.su.conventional)

  attach(data.ch.su.kinetics)
  # Extract the relevant columns
  times.kinetics = TimeSpent
  attempts.kinetics = as.numeric(Attempts)
  distance.kinetics = TotalDistance
  area.kinetics = TotalArea
  detach(data.ch.su.kinetics)

  # t-test
  t.times = ttest(times.conventional, times.kinetics, n.attacks)
  t.attempts = ttest(attempts.conventional, attempts.kinetics, n.attacks)
  t.distance = ttest(distance.conventional, distance.kinetics, n.attacks)
  t.area = ttest(area.conventional, area.kinetics, n.attacks)

  # Append to 'all' vectors
  t.all.times = c(t.all.times, t.times)
  t.all.attempts = c(t.all.attempts, t.attempts)
  t.all.distance = c(t.all.distance, t.distance)
  t.all.area = c(t.all.area, t.area)
  all.times.conventional = c(all.times.conventional, times.conventional)
  all.times.kinetics = c(all.times.kinetics, times.kinetics)
  all.attempts.conventional = c(all.attempts.conventional, attempts.conventional)
  all.attempts.kinetics = c(all.attempts.kinetics, attempts.kinetics)
  all.distance.conventional = c(all.distance.conventional, distance.conventional)
  all.distance.kinetics = c(all.distance.kinetics, distance.kinetics)
  all.area.conventional = c(all.area.conventional, area.conventional)
  all.area.kinetics = c(all.area.kinetics, area.kinetics)
}

# Calculate global mean values
mean.times.all = mean(all.times)
mean.attempts.all = mean(all.attempts)
mean.distance.all = mean(all.distance)
mean.area.all = mean(all.area)
mean.times.conventional = mean(all.times.conventional)
mean.times.kinetics = mean(all.times.kinetics)
mean.attempts.conventional = mean(all.attempts.conventional)
mean.attempts.kinetics = mean(all.attempts.kinetics)
mean.distance.conventional = mean(all.distance.conventional)
mean.distance.kinetics = mean(all.distance.kinetics)
mean.area.conventional = mean(all.area.conventional)
mean.area.kinetics = mean(all.area.kinetics)

data.tests = data.frame(
  TimeTests=t.all.times,
  AttemptsTests=t.all.attempts,
  DistanceTests=t.all.distance,
  AreaTests=t.all.area)

write.csv(data.tests, "tests.csv")

data.means = data.frame(
  MeanTimesAll=mean.times.all,
  MeanAttemptsAll=mean.attempts.all,
  MeanDistanceAll=mean.distance.all,
  MeanAreaAll=mean.area.all,
  MeanTimesConventional=mean.times.conventional,
  MeanTimesKinetics=mean.times.kinetics,
  MeanAttemptsConventional=mean.attempts.conventional,
  MeanAttemptsKinetics=mean.attempts.kinetics,
  MeanDistanceConventional=mean.distance.conventional,
  MeanDistanceKinetics=mean.distance.kinetics,
  MeanAreaConventional=mean.area.conventional,
  MeanAreaKinetics=mean.area.kinetics
)

write.csv(data.means, "means.csv")


