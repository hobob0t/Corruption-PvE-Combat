//Strike Drone Mk.II AI Script

//Configuration
string remoteControlSpaceName = "Remote Control (Space)";
string remoteControlGravityName = "Remote Control (Gravity)";
double noPlayerDespawnDist = 20000;

//Positions
Vector3D closestPlayer = new Vector3D(0,0,0);
Vector3D dronePosition = new Vector3D(0,0,0);
Vector3D originPosition = new Vector3D(0,0,0);
Vector3D planetLocation = new Vector3D(0,0,0);
Vector3D alignmentPosition = new Vector3D(0,0,0);

//Distances
double distanceDroneToPlayer = 0;
double distanceDroneToOrigin = 0;
double distancePlayerToOrigin = 0;

//Bool Checks
bool droneIsNPC = false;
bool inNaturalGravity = false;

//Global Blocks List & Remote Control
List<IMyTerminalBlock> blockList = new List<IMyTerminalBlock>();
List<IMyGyro> gyroList = new List<IMyGyro>();
IMyRemoteControl remoteControlSpace;
IMyRemoteControl remoteControlGravity;
IMyRemoteControl remoteControl;

string currentMode = "Space-Seeking";
int timeOut = 0;
bool autoRotate = false;
int tickIncrement = 10;
int tickCounter = 0;
Random rnd = new Random();

public Program(){
	
	Runtime.UpdateFrequency = UpdateFrequency.Update10;
	
}


void DroneBehaviour(string argument){
	
	Echo(currentMode);
	bool cameraDamaged = false;
	TryIceRefill();
	
	if(inNaturalGravity == false && currentMode.Contains("Space") == false){
		
		currentMode = "Space-Seeking";
		
	}
	
	if(inNaturalGravity == true && currentMode.Contains("Gravity") == false){
		
		currentMode = "Gravity-Seeking";
		
	}
	
	if(cameraDamaged == true){
		
		currentMode = "Gravity-FinalRun";
		
	}
	
	///////////////////////////////////////
	//SPACE BEHAVIOUR
	///////////////////////////////////////
	
	if(currentMode == "Space-Seeking"){
		
		SetDestination(closestPlayer, false, 100);
		
		if(distanceDroneToPlayer > 900){
			
			return;
			
		}
		
		var scannedTarget = CameraRayCast(remoteControl.WorldMatrix.Forward, (float)distanceDroneToPlayer, out cameraDamaged);
		
		if(scannedTarget.IsEmpty() == true){
			
			WeaponActivation("Shoot_Off");
			
			if(distanceDroneToPlayer < 500){
				
				Vector3D randomDir = RandomDirection();
				Vector3D realignCoords = randomDir * 1050 + closestPlayer;
				
				if(MeasureDistance(realignCoords, dronePosition) > MeasureDistance(realignCoords * -1, dronePosition)){
					
					realignCoords = realignCoords * -1;
					
				}
					
				timeOut = 30;
				SetDestination(realignCoords, true, 100);
				WeaponActivation("Shoot_Off");
				currentMode = "Space-Realign";
				return;
				
			}
			
			return;
			
		}
		
		if(scannedTarget.HitPosition == null){
			
			return;
			
		}
		
		if(scannedTarget.Type == MyDetectedEntityType.Asteroid || scannedTarget.Type == MyDetectedEntityType.Planet){
				
			WeaponActivation("Shoot_Off");
			
		}
		
		if(distanceDroneToPlayer < 500 || MeasureDistance((Vector3D)scannedTarget.HitPosition, dronePosition) < 500){
			
			Vector3D randomDir = RandomDirection();
			Vector3D realignCoords = randomDir * 1050 + closestPlayer;
			
			if(MeasureDistance(realignCoords, dronePosition) > MeasureDistance(realignCoords * -1, dronePosition)){
				
				realignCoords = realignCoords * -1;
				
			}
				
			timeOut = 30;
			SetDestination(realignCoords, true, 100);
			WeaponActivation("Shoot_Off");
			currentMode = "Space-Realign";
			return;

		}
				
		if(MeasureDistance((Vector3D)scannedTarget.HitPosition, dronePosition) < 800 || scannedTarget.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies){
			
			WeaponActivation("Shoot_On");
			
		}else{
			
			WeaponActivation("Shoot_Off");
			
		}
		
	}
	
	if(currentMode == "Space-Realign"){
		
		timeOut--;
		var coords = remoteControl.CurrentWaypoint.Coords;
		
		if(MeasureDistance(dronePosition, coords) < 50 || timeOut <= 0){
			
			currentMode = "Space-Seeking";
			SetDestination(closestPlayer, false, 100);
			return;
			
		}
		
	}
	
	///////////////////////////////////////
	//GRAVITY BEHAVIOUR
	///////////////////////////////////////
	
	
	if(currentMode == "Gravity-Seeking"){
		
		//Set The Random Target Here
		var scannedTarget = CameraRayCast(remoteControl.WorldMatrix.Down, 10, out cameraDamaged);
		var up = Vector3D.Normalize(closestPlayer - planetLocation);
		var forward = Vector3D.CalculatePerpendicularVector(up);
		var matrixCoords = CreateDirectionAndTarget(planetLocation, closestPlayer, closestPlayer, 1200);
		var targetMatrix = MatrixD.CreateWorld(matrixCoords, forward, up);
		double targetX = (double)rnd.Next(-500, 500);
		double targetZ = (double)rnd.Next(-500, 500);
		var targetOffset = new Vector3D(targetX, 0, targetZ);
		alignmentPosition = Vector3D.Transform(targetOffset, targetMatrix);
		currentMode = "Gravity-Aligning";
		
	}
	
	if(currentMode == "Gravity-Aligning"){
		
		if(MeasureDistance(alignmentPosition, dronePosition) > 50){
			
			SetDestination(alignmentPosition, false, 100);
			return;
			
		}
				
		remoteControl.SetAutoPilotEnabled(false);
		currentMode = "Gravity-RotateStart";
		
	}
	
	if(currentMode == "Gravity-RotateStart"){
		
		if(remoteControl.GetShipSpeed() < 1){
			
			AutoRotateActivation(true);
			timeOut = 0;
			currentMode = "Gravity-RotateEnd";
			return;
			
		}
	
	}
	
	if(currentMode == "Gravity-RotateEnd"){
		
		timeOut++;
		
		if(timeOut < 7){
			
			return;
						
		}
		
		currentMode = "Gravity-Engaging";
		var scannedTarget = CameraRayCast(remoteControl.WorldMatrix.Down, 2500, out cameraDamaged);
		
		if(scannedTarget.IsEmpty() == true){
			
			AutoRotateActivation(false);
			currentMode = "Gravity-Seeking";
			return;
			
		}
		
		if(scannedTarget.Relationship != MyRelationsBetweenPlayerAndBlock.Enemies){
			
			AutoRotateActivation(false);
			currentMode = "Gravity-Seeking";
			return;
			
		}
		
		remoteControl.SetAutoPilotEnabled(false);
		OverrideThrust(true, remoteControl.WorldMatrix.Down, 100);
		
	}

	if(currentMode == "Gravity-Engaging"){
		
		var scannedTarget = CameraRayCast(remoteControl.WorldMatrix.Down, 2000, out cameraDamaged);
		
		if(scannedTarget.IsEmpty() == true){
			
			AutoRotateActivation(false);
			OverrideThrust(false, remoteControl.WorldMatrix.Down, 0);
			currentMode = "Gravity-Seeking";
			WeaponActivation("Shoot_Off");
			return;
			
		}
		
		if(scannedTarget.HitPosition == null){
			
			return;
			
		}
		
		if(scannedTarget.Relationship != MyRelationsBetweenPlayerAndBlock.Enemies){
			
			AutoRotateActivation(false);
			OverrideThrust(false, remoteControl.WorldMatrix.Down, 0);
			currentMode = "Gravity-Seeking";
			WeaponActivation("Shoot_Off");
			return;
			
		}
		
		if(MeasureDistance(dronePosition, (Vector3D)scannedTarget.HitPosition) < 800){
			
			WeaponActivation("Shoot_On");
			
		}else{
			
			WeaponActivation("Shoot_Off");
			
		}
		
		if(MeasureDistance(dronePosition, (Vector3D)scannedTarget.HitPosition) < 550){
			
			AutoRotateActivation(false);
			OverrideThrust(false, remoteControl.WorldMatrix.Down, 0);
			currentMode = "Gravity-Seeking";
			WeaponActivation("Shoot_Off");
			return;
			
		}
		
	}
	
	if(currentMode == "Gravity-FinalRun"){
		
		AutoRotateActivation(false);
		OverrideThrust(false, remoteControl.WorldMatrix.Down, 0);
		SetDestination(closestPlayer, false, 100);
		
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
	
	TryRotate();
	
	tickCounter += tickIncrement;
	
	if(tickCounter < 60){
		
		return;
		
	}
	
	tickCounter = 0;
	
	//Reset Block List and Check For Remote Control
	remoteControlSpace = GridTerminalSystem.GetBlockWithName(remoteControlSpaceName) as IMyRemoteControl;
	remoteControlGravity = GridTerminalSystem.GetBlockWithName(remoteControlGravityName) as IMyRemoteControl;
	
	if(remoteControlSpace == null && remoteControlGravity == null){
		
		Echo("No remote control found. Aborting Script");
		return;
		
	}
	
	if(remoteControlSpace.IsFunctional == false && remoteControlGravity.IsFunctional == false){
		
		Echo("No remote control found. Aborting Script");
		return;
		
	}
	
	ResetBlockList();
	
	
	inNaturalGravity = false;
	if(remoteControlGravity != null){
		
		if(remoteControlGravity.IsFunctional == true){
			
			if(remoteControlGravity.TryGetPlanetPosition(out planetLocation) == true){
				
				remoteControl = remoteControlGravity;
				remoteControlSpace?.SetAutoPilotEnabled(false);
				inNaturalGravity = true;
				
			}
			
		}

	}
	
	if(inNaturalGravity == false){
		
		remoteControl = remoteControlSpace;
		remoteControlGravity?.SetAutoPilotEnabled(false);
		
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
	
	if(gyroList.Count == 0){
		
		GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyroList);
		
	}
	
	//Execute Custom Drone Behaviour
	DroneBehaviour(argument);
	
}

double MeasureDistance(Vector3D point_a, Vector3D point_b){
	
	double result = Math.Round( Vector3D.Distance( point_a, point_b ), 2 );
	return result;
	
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

MyDetectedEntityInfo CameraRayCast(Vector3D direction, float scanDistance, out bool noCamera){
	
	noCamera = true;
	MyDetectedEntityInfo scanResults = new MyDetectedEntityInfo();
	
	foreach(var block in blockList){
		
		var camera = block as IMyCameraBlock;
		
		if(camera == null || block.IsFunctional == false){
			
			continue;
			
		}
		
		//TODO: Figure out if camera direction is Forward or Up
		if(camera.WorldMatrix.Forward != direction){
			
			continue;
			
		}
		
		noCamera = false;
		camera.EnableRaycast = true;
				
		if(camera.CanScan(scanDistance) == false){
			
			continue;
			
		}
		
		scanResults = camera.Raycast(scanDistance, 0, 0);
		return scanResults;
		
	}
	
	return scanResults;
	
}

void WeaponActivation(string weaponMode){
	
	foreach(var block in blockList){
		
		var weapon = block as IMyUserControllableGun;
		
		if(weapon == null || block.IsFunctional == false){
			
			continue;
			
		}
		
		weapon.ApplyAction(weaponMode);
		
	}
	
}

void OverrideThrust(bool enableOverride, Vector3D direction, float thrustModifier){
	
	foreach(var block in blockList){
		
		var thruster = block as IMyThrust;
		
		if(thruster == null || block.IsFunctional == false){
			
			continue;
			
		}
		
		if(enableOverride == true){
			
			if(thruster.WorldMatrix.Forward == direction * -1){
				
				thruster.Enabled = true;
				float maxthrust = thruster.MaxThrust;
				thruster.ThrustOverridePercentage = thrustModifier;
			
			}
			
			if(thruster.WorldMatrix.Forward == direction){
			
				thruster.Enabled = false;
			
			}
			
		}else{
			
			thruster.Enabled = true;
			thruster.SetValueFloat("Override", 0);
			
		}
		
	}
	
}

void ArmWarheads(bool enabled){
	
	foreach(var block in blockList){
		
		var weapon = block as IMyWarhead;
		
		if(weapon == null || block.IsFunctional == false){
			
			continue;
			
		}
		
		weapon.IsArmed = enabled;
		
	}
	
}

void SelfDestruct(string weaponMode){
	
	foreach(var block in blockList){
		
		var weapon = block as IMyWarhead;
		
		if(weapon == null || block.IsFunctional == false){
			
			continue;
			
		}
	
		if(weaponMode == "Detonate"){
			
			weapon.IsArmed = true;
			weapon.Detonate();
			
		}
		
	}
	
}

Vector3D RandomDirection(){
	
	Vector3D randomDir = new Vector3D(0,0,0);
	randomDir.X = RandomNumberBetween(-0.999999, 0.999999);
	randomDir.Y = RandomNumberBetween(-0.999999, 0.999999);
	randomDir.Z = RandomNumberBetween(-0.999999, 0.999999);
	randomDir = Vector3D.Normalize(randomDir);
	return randomDir;
	
	
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

Vector3D randomRealignPosition(){
	
	MatrixD rcMatrix = MatrixD.CreateWorld(remoteControl.GetPosition(), remoteControl.WorldMatrix.Forward, remoteControl.WorldMatrix.Up);
	Vector3D coords = new Vector3D(0,0,0);
	coords.X = (double)rnd.Next(-500, 500);
	coords.Y = 600;
	coords.Z = (double)rnd.Next(-500, 500);
	var finalCoords = Vector3D.Transform(coords, rcMatrix);
	return finalCoords;
	
}

void ScriptSpeedRegular(){
	
	Runtime.UpdateFrequency |= UpdateFrequency.Update10;
	Runtime.UpdateFrequency &= ~UpdateFrequency.Update1;
	tickIncrement = 10;
	tickCounter = 0;
	
}

void ScriptSpeedFast(){
	
	Runtime.UpdateFrequency |= UpdateFrequency.Update1;
	Runtime.UpdateFrequency &= ~UpdateFrequency.Update10;
	tickIncrement = 1;
	tickCounter = 0;
	
}

void AutoRotateActivation(bool rotateMode){
	
	if(rotateMode == false){
		
		autoRotate = false;
		ScriptSpeedRegular();
		
	}else{
		
		autoRotate = true;
		ScriptSpeedFast();
		
	}
	
}

void TryRotate(){
		
	if(autoRotate == true){
		
		if(remoteControlSpace == null){
		
			return;
		
		}
		
		double yaw = 0;
		double pitch = 0;
		var targetDir = Vector3D.Normalize(remoteControl.GetPosition() - closestPlayer);
		GetRotationAngles(targetDir * -1, (IMyTerminalBlock)remoteControlSpace, out yaw, out pitch);
		ApplyGyroOverride(pitch, yaw, 0, gyroList, (IMyTerminalBlock)remoteControlSpace);
		
	}	
	
	
	
}

Vector3D CreateDirectionAndTarget(Vector3D startDirCoords, Vector3D endDirCoords, Vector3D startPathCoords, double pathDistance){
	
	var direction = Vector3D.Normalize(endDirCoords - startDirCoords);
	var coords = direction * pathDistance + startPathCoords;
	return coords;
	
}

/*
/// Whip's Get Rotation Angles Method v12 - 2/16/18 ///
Dependencies: VectorAngleBetween()
* Fix to solve for zero cases when a vertical target vector is input
* Fixed straight up case
* Fixed sign on straight up case
* Converted math to local space
*/
void GetRotationAngles(Vector3D targetVector, IMyTerminalBlock reference, out double yaw, out double pitch)
{
    var localTargetVector = Vector3D.TransformNormal(targetVector, MatrixD.Transpose(reference.WorldMatrix));
    var flattenedTargetVector = new Vector3D(localTargetVector.X, 0, localTargetVector.Z);
    
    yaw = VectorAngleBetween(Vector3D.Forward, flattenedTargetVector) * Math.Sign(localTargetVector.X); //right is positive
    if (Math.Abs(yaw) < 1E-6 && localTargetVector.Z > 0) //check for straight back case
        yaw = Math.PI;
    
    if (Vector3D.IsZero(flattenedTargetVector)) //check for straight up case
        pitch = MathHelper.PiOver2 * Math.Sign(localTargetVector.Y);
    else
        pitch = VectorAngleBetween(localTargetVector, flattenedTargetVector) * Math.Sign(localTargetVector.Y); //up is positive
}

double VectorAngleBetween(Vector3D a, Vector3D b) //returns radians 
{
    if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
        return 0;
    else
        return Math.Acos(MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1));
}

//Whip's ApplyGyroOverride Method v9 - 8/19/17
void ApplyGyroOverride(double pitch_speed, double yaw_speed, double roll_speed, List<IMyGyro> gyro_list, IMyTerminalBlock reference) 
{ 
    var rotationVec = new Vector3D(-pitch_speed, yaw_speed, roll_speed); //because keen does some weird stuff with signs 
    var shipMatrix = reference.WorldMatrix;
    var relativeRotationVec = Vector3D.TransformNormal(rotationVec, shipMatrix); 

    foreach (var thisGyro in gyro_list) 
    { 
	
		if(thisGyro == null){
			
			continue;
			
		}
		
        var gyroMatrix = thisGyro.WorldMatrix;
        var transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(gyroMatrix)); 
 
        thisGyro.Pitch = (float)transformedRotationVec.X;
        thisGyro.Yaw = (float)transformedRotationVec.Y; 
        thisGyro.Roll = (float)transformedRotationVec.Z; 
        thisGyro.GyroOverride = true; 
    } 
}