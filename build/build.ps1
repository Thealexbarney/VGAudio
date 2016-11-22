properties { 

}

framework '4.6x86'

task default -depends Test

task Clean {

}

task Build -depends Clean {
  
}

task Package -depends Build {
  
}

task Test -depends Package {
      
}
