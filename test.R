csv.files = Sys.glob("csv/*.csv")
# uncomment this when the deprecated log should not be included
csv.files = c(csv.files, "csv/deprecated/DeprecatedUserLog.csv")

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

custom.boxplot = function(x, y, min, max, title="TITLE", subtitle="subtitle") {
  boxplot(x[x >= min & x <= max], y[y >= min & y <= max],
    names=c("Big Joystick", "Small Joystick"),
    pars=list(
      boxwex=0.8, staplewex=0.5, outwex=0.5
    )
  )
  title(title, subtitle)
}

custom.spread = function(x, min, max, width=0.2,
  title="TITLE", subtitle="subtitle", rug=TRUE) {
  hist(x[x >= min & x <= max],
    seq(min, max, width),
    main=title, sub=subtitle
  )
  if (rug == TRUE) {
    rug(x)
  }
}


custom.boxplot(
  all.times.conventional,
  all.times.kinetics,
  0.0, 50.0,
  "Time spent on attacks (s)", "Untrimmed"
)

custom.boxplot(
  all.times.conventional,
  all.times.kinetics,
  0.0, 20.0,
  "Time spent on attacks (s)", "0.0 <= t <= 20.0"
)

custom.boxplot(
  all.times.conventional,
  all.times.kinetics,
  0.0, 6.0, 
  "Time spent on attacks (s)", "0.0 <= t <= 6.0"
)

custom.boxplot(
  all.attempts.conventional,
  all.attempts.kinetics,
  0, 10000,
  "Number of attempts per attack", "Untrimmed"
)

custom.boxplot(
  all.attempts.conventional,
  all.attempts.kinetics,
  0, 10,
  "Number of attempts per attack", "0.0 <= x <= 10"
)

custom.boxplot(
  all.distance.conventional,
  all.distance.kinetics,
  0, 10000,
  "Finger distance traveled (pixels)", ""
)

custom.boxplot(
  all.area.conventional,
  all.area.kinetics,
  0, 1000000,
  "Screen area used (pixels²)", ""
)

custom.spread(all.times, 0.0, 50.0, 1.0,
  "Time spent on attacks (s)", "Untrimmed")

custom.spread(all.times, 0.0, 20.0, 0.5,
  "Time spent on attacks (s)", "0.0 <= t <= 20")

custom.spread(all.times, 0.0, 6.0, 0.2,
  "Time spent on attacks (s)", "0.0 <= t <= 6.0")

custom.spread(all.attempts, 0.0, 35.0, 1.0,
  "Attempts per attack", "Untrimmed", FALSE)

custom.spread(all.attempts, 0.0, 10.0, 1.0,
  "Attempts per attack", "0.0 <= x <= 10.0", FALSE)

custom.spread(all.distance, 0.0, 1500.0, 10.0,
  "Finger distance traveled (pixels)", "", FALSE)

custom.spread(all.area, 0.0, 1e+05, 500.0,
  "Screen area used (pixels²)", "", FALSE)

  
  
