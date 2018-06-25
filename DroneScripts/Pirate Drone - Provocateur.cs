//Provocateur Drone AI Script

double noPlayerDespawnDist = 20000;

//Positions
Vector3D closestPlayer = new Vector3D(0,0,0);
Vector3D dronePosition = new Vector3D(0,0,0);
Vector3D originPosition = new Vector3D(0,0,0);
Vector3D planetLocation = new Vector3D(0,0,0);

//Distances
double distanceDroneToPlayer = 0;
double distanceDroneToOrigin = 0;
double distancePlayerToOrigin = 0;

//Bool Checks
bool droneIsNPC = false;
bool inNaturalGravity = false;

//Global Blocks List & Remote Control
List<IMyTerminalBlock> blockList = new List<IMyTerminalBlock>();
IMyRemoteControl remoteControl;

string lastMessageSent = "Default";

string currentMode = "Approach";
bool scriptInit = false;
string greeting = "";
string retreat = "";
string success = "";
List<string> insultList = new List<string>();
bool spawnedReinforcements = false;
int insultTimer = 0;
int insultIndex = 0;
int timeOut = 0;
Random rnd = new Random();

int tickIncrement = 10;
int tickCounter = 0;

public Program(){
	
	Runtime.UpdateFrequency = UpdateFrequency.Update10;
	
}

void DroneBehaviour(string argument){
	
	TryIceRefill();
	
	if(scriptInit == false){
		
		scriptInit = true;
		var randNum = rnd.Next(0,5);
		
		if(Me.CubeGrid.CustomName != "(NPC-CPC) Provocateur Drone"){
			
			randNum = 0;
			
		}
		
		if(randNum == 0 || Me.CubeGrid.CustomName == "(NPC-CPC) Challenger Drone"){
			
			Me.CubeGrid.CustomName = "(NPC-CPC) Challenger Drone";
			ChangeAntennaName("Challenger Drone");
			greeting = "Today is my lucky day. I get to bear witness to your demise.";
			retreat = "I suppose I cannot expect much action from cowards. I was really hoping you would have proved me wrong.";
			success = "I admire your courage, even if it will be your undoing.";
			insultList.Add("Do you know what a lotus is? Would you like to see one? I can arrange that.");
			insultList.Add("That thing you're building... It would look better on fire.");
			insultList.Add("Maybe I should broadcast your position to the rest of the drones in the area.");
			insultList.Add("You have no chance against me. Your armament is pathetic.");
			insultList.Add("Are you afraid of spiders? It'd be a shame if that was the case.");
			
		}
		
		if(randNum == 1 || Me.CubeGrid.CustomName == "(NPC-CPC) Critique Drone"){
			
			Me.CubeGrid.CustomName = "(NPC-CPC) Critique Drone";
			ChangeAntennaName("Critique Drone");
			greeting = "Well, well, well... It appears I've stumbled upon some talentless meatbags.";
			retreat = "I simply cannot waste anymore of my time with this nonsense.";
			success = "Did I touch a nerve? Your fragile ego will be your undoing.";
			insultList.Add("That design you're building looks familiar, {Player}. Did you steal it from someone more talented?");
			insultList.Add("I'm convinced you got your engineering license from a box of cereal.");
			insultList.Add("It looks like you're building a re-creation of a primitive civilization.");
			insultList.Add("Your choice in color schemes is very uninspired.");
			insultList.Add("I've seen more creativity from children and their building blocks.");
			
		}
		
		if(randNum == 2 || Me.CubeGrid.CustomName == "(NPC-CPC) Merchant Drone"){
			
			Me.CubeGrid.CustomName = "(NPC-CPC) Merchant Drone";
			ChangeAntennaName("Merchant Drone");
			greeting = "I've got some tech you may be interested in. Lower your weapons and come take a look!";
			retreat = "Guess you can't recognize a good deal when it's sitting right at your door-step. Oh well, someone else will buy from me if you won't.";
			success = "You really thought I would sell our tech to YOU? Your greed will be be your undoing.";
			insultList.Add("I'd be willing to sell the short-range teleport technology you may have encountered.");
			insultList.Add("I have a prototype shielding module for sale, if the price is right.");
			insultList.Add("How about some long range rockets? I've got plenty of those available!");
			insultList.Add("Want a remote hacking module? I can hook you up!");
			insultList.Add("It's not cheap, but this laser cannon module will make you unstoppable!");
			
		}
		
		if(randNum == 3 || Me.CubeGrid.CustomName == "(NPC-CPC) Seduction Drone"){
			
			Me.CubeGrid.CustomName = "(NPC-CPC) Seduction Drone";
			ChangeAntennaName("Seduction Drone");
			greeting = "Come to me and I'll show you a time you'll never forget.";
			retreat = "Ignore me? Well then it's time for a hard truth. It's not me, it's you.";
			success = "You should have been thinking with your head, not with the other thing.";
			insultList.Add("I'm ready to dock with your connector.");
			insultList.Add("I've got a place you can put your steel tubes.");
			insultList.Add("Is that an extended piston, or are you just happy to see me?");
			insultList.Add("My reactors are ready for your fuel");
			insultList.Add("After we're done, those batteries are gonna need a recharge.");
			
		}
		
		if(randNum == 4 || Me.CubeGrid.CustomName == "(NPC-CPC) Totally-Not-A-Trap Drone"){
			
			Me.CubeGrid.CustomName = "(NPC-CPC) Totally-Not-A-Trap Drone";
			ChangeAntennaName("Totally-Not-A-Trap Drone");
			greeting = "I think I'll take a break over here. All this valuable cargo is weighing me down.";
			retreat = "Who am I kidding? Nobody was going to fall for this...";
			success = "Wait, you really fell for it!? There's no hope for humanity...";
			insultList.Add("I sure hope nobody tries to steal all my platinum.");
			insultList.Add("It would be bad if I got attacked, I've used up all my ammo already.");
			insultList.Add("At least I've got lots of fuel. More than I'd ever need!");
			insultList.Add("I still can't believe they trusted me to deliver this cargo alone. I won't let them down though!");
			insultList.Add("It does get lonely out here though. It would be nice if somebody stopped by for a friendly visit.");
			
		}
		
	}
	
	if(currentMode == "Approach"){
		
		SetDestination(closestPlayer, false, 100);
		
		if(distanceDroneToPlayer < 4000){
			
			remoteControl.SetAutoPilotEnabled(false);
			currentMode = "Taunt";
			TryChat(greeting);
			
		}
		
	}
	
	if(currentMode == "Taunt"){
		
		insultTimer++;
		
		if(insultTimer >= 120){
			
			insultTimer = 0;
			
			if(insultIndex >= insultList.Count){
				 
				TryChat(retreat);
				currentMode = "Retreat";
				return;
				 
			}
			 
			TryChat(insultList[insultIndex]);
			insultIndex++;
			
		}
		
		if(distanceDroneToPlayer < 1200){
			
			Me.CubeGrid.CustomName = "(NPC-CPC) Provocateur Drone";
			ChangeAntennaName("Provocateur Drone");
			TryChat(success);
			spawnedReinforcements = Me.GetValue<bool>("CorruptionBossSpawn");
			currentMode = "Ambush";
			
		}
		
	}
	
	if(currentMode == "Ambush"){
		
		if(spawnedReinforcements == true){
			
			currentMode = "Retreat";
			
		}else{
			
			spawnedReinforcements = Me.GetValue<bool>("CorruptionBossSpawn");
			
		}
		
	}
	
	if(currentMode == "Retreat"){
		
		if(distanceDroneToPlayer > 4000){
			
			TryDespawn(remoteControl, false);
			
		}
		
		if(inNaturalGravity == false){
			
			var retreatCoords = CreateDirectionAndTarget(closestPlayer, dronePosition, dronePosition, 5000);
			SetDestination(retreatCoords, true, 100);
			
		}else{
			
			var retreatCoords = CreateDirectionAndTarget(planetLocation, dronePosition, dronePosition, 5000);
			SetDestination(retreatCoords, true, 100);
			
		}
		
		
		
	}
	
}

void Main(string argument){
	
	try{
		
		if(Me.GetValue<bool>("CorruptionOwnerCheck") == false){
	
			Echo("Non-NPC");
			return;
		
		}
		
	}catch(Exception exc){
	
		//ModAPI Despawn Failed
	
	}
	
	tickCounter += tickIncrement;
	
	if(tickCounter < 60){
		
		return;
		
	}
	
	tickCounter = 0;
	
	//Reset Block List and Check For Remote Control
	remoteControl = GridTerminalSystem.GetBlockWithName("Remote Control") as IMyRemoteControl;
			
	inNaturalGravity = false;
	if(remoteControl != null){
		
		if(remoteControl.IsFunctional == true){
			
			if(remoteControl.TryGetPlanetPosition(out planetLocation) == true){
				
				inNaturalGravity = true;
				
			}
			
		}

	}else{
		
		return;
		
	}
	
	//Ensure Remote Control Is NPC Owned & Get Player Location
	droneIsNPC = remoteControl.GetNearestPlayer(out closestPlayer);

	if(droneIsNPC == false){
		
		Echo("Drone is not controlled by an NPC");
		return;
		
	}
	
	OriginSetup();
		
	//Get Other Location Data
		
	dronePosition = remoteControl.GetPosition();

	//Calculate Distances
	distanceDroneToPlayer = MeasureDistance(dronePosition, closestPlayer);
	distanceDroneToOrigin = MeasureDistance(dronePosition, originPosition);
	distancePlayerToOrigin = MeasureDistance(closestPlayer, originPosition);
	
	if(distanceDroneToPlayer > noPlayerDespawnDist){
		
		TryDespawn(remoteControl, false);
		
	}
	
	//Execute Custom Drone Behaviour
	ResetBlockList();
	DroneBehaviour(argument);
	
}

double MeasureDistance(Vector3D point_a, Vector3D point_b){
	
	double result = Math.Round( Vector3D.Distance( point_a, point_b ), 2 );
	return result;
	
}

void ChangeAntennaName(string newName){
	
	foreach(var block in blockList){
		
		var antenna = block as IMyRadioAntenna;
		
		if(antenna != null){
			
			antenna.CustomName = newName;
			
		}
		
	}
	
}

void ResetBlockList(){
	
	blockList.Clear();
	GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blockList);
	
}

void OriginSetup(){
	
	if(droneIsNPC == false){
		
		originPosition = new Vector3D(0,0,0);
		
	}
	
	if(originPosition == new Vector3D(0,0,0) && droneIsNPC == true){
		
		originPosition = remoteControl.GetPosition();
		
	}
	
}

void SetDestination(Vector3D coords, bool collisionModeEnable, float speedLimit){
	
	remoteControl.ClearWaypoints();
	remoteControl.AddWaypoint(coords, "Destination");
	remoteControl.FlightMode = FlightMode.OneWay;
	remoteControl.SetAutoPilotEnabled(true);
	remoteControl.SetCollisionAvoidance(collisionModeEnable);
	remoteControl.SpeedLimit = speedLimit;
	
}

Vector3D RandomDirection(){
	
	Vector3D randomDir = new Vector3D(0,0,0);
	randomDir.X = RandomNumberBetween(-0.999999, 0.999999);
	randomDir.Y = RandomNumberBetween(-0.999999, 0.999999);
	randomDir.Z = RandomNumberBetween(-0.999999, 0.999999);
	randomDir = Vector3D.Normalize(randomDir);
	return randomDir;
	
	
}

Vector3D CreateDirectionAndTarget(Vector3D startDirCoords, Vector3D endDirCoords, Vector3D startPathCoords, double pathDistance){
	
	var direction = Vector3D.Normalize(endDirCoords - startDirCoords);
	var coords = direction * pathDistance + startPathCoords;
	return coords;
	
}

double RandomNumberBetween(double minValue, double maxValue){
	
    var next = rnd.NextDouble();
    return minValue + (next * (maxValue - minValue));
	
}

void TryDespawn(IMyRemoteControl remoteControl, bool gravity){
	
	Vector3D planetPosition = new Vector3D(0,0,0);
	if(remoteControl.TryGetPlanetPosition(out planetPosition) == true || gravity == false){
	
		try{
		
			bool despawn = Me.GetValue<bool>("CorruptionDroneDespawn");
		
		}catch(Exception exc){
		
			//ModAPI Despawn Failed
		
		}
	
	}

}

void TryIceRefill(){

	try{
		
		foreach(var block in blockList){
			
			var gasGenerator = block as IMyGasGenerator;
			
			if(gasGenerator != null){
				
				var inv = gasGenerator.GetInventory(0);
				
				if(inv.CurrentVolume == 0){
					
					bool refill = Me.GetValue<bool>("CorruptionIceRefill");
					
				}
				
			}
			
		}
		
	}catch(Exception exc){
	
		//ModAPI Refill Failed
	
	}

}

void TryChat(string message){
	
	if(message == lastMessageSent){
		
		return;
		
	}
	
	try{
		
		Me.CustomData = message;
		bool despawn = Me.GetValue<bool>("CorruptionDroneChat");
		lastMessageSent = message;
	
	}catch(Exception exc){
	
		//ModAPI Chat Failed
	
	}

}
