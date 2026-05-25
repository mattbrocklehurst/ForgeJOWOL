
                   ForgeJO CI C# Watcher Service
 
  Author - Matt Brocklehurst / 2026
           Copyright (C) 2026 Matt Brocklehurst
 
  Simple service designed to sit and run monitoring if any CI jobs are queued on
  ForgeJO, the CI runner machine spends most of its life sleeping, if a CI job is
  waiting to be run, this service first checks to see if its alive (ping), if not
  we send the magic WOL packet to it.
