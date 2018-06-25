//Assault Drone Mk.II AI Script

//Configuration
double noPlayerDespawnDist = 20000;

//Positions
Vector3D closestPlayer = new Vector3D(0,0,0);
Vector3D dronePosition = new Vector3D(0,0,0);
Vector3D originPosition = new Vector3D(0,0,0);
Vector3D planetLocation = new Vector3D(0,0,0);
Vector3D alignmentPosition = new Vector3D(0,0,0);
Vector3D currentTarget = new Vector3D(0,0,0);

//Distances
double distanceDroneToPlayer = 0;
double distanceDroneToOrigin = 0;
double distancePlayerToOrigin = 0;

//Bool Checks
bool droneIsNPC = false;
bool inNaturalGravity = false;
bool rotateToggle = false;

//ShipNames
string shipShields = "(NPC-CPC) Shielded Assault Drone";
string shipNoShields = "(NPC-CPC) Assault Drone";
string antennaShields = "Shielded Assault Drone";
string antennaNoShields = "Assault Drone";

//Global Blocks List & Remote Control
List<IMyTerminalBlock> blockList = new List<IMyTerminalBlock>();
List<IMyGyro> gyroList = new List<IMyGyro>();
IMyRemoteControl remoteControl;

long coordinatorId = 0;
Vector3D coordinatorPosition = new Vector3D(0,0,0);
int tryGetCoordinatorAttempts = 0;
bool droneVulnerable = true;
bool coordinatorProximity = false;

int timeOut = 0;
bool autoRotate = false;
int tickIncrement = 10;
int tickCounter = 0;
Random rnd = new Random();
string currentMode = "GetCoordinator";

public Program(){
	
	Runtime.UpdateFrequency = UpdateFrequency.Update10;
	
}


void DroneBehaviour(string argument){
	
	bool cameraDamaged = false;
	TryIceRefill();
	var shieldRegister = Me.GetValue<bool>("CorruptionShieldRegister");
	
	if(coordinatorId != 0){
		
		//Update Position From Entity ID
		Me.CustomData = coordinatorId.ToString();
		coordinatorPosition = Me.GetValue<Vector3D>("CorruptionTrackEntity");
		
		if(coordinatorPosition == new Vector3D(0,0,0)){
			
			ChangeAntennaName(antennaNoShields);
			Me.CubeGrid.CustomName = shipNoShields;
			coordinatorId = 0;
			droneVulnerable = true;
			BlockToggle("Shield Light", false);
			Me.CustomData = droneVulnerable.ToString();
			bool result = Me.GetValue<bool>("CorruptionInvincibility");
			currentMode = "AttackPlayer";
			coordinatorProximity = false;
			return;
			
		}
		
		if(MeasureDistance(coordinatorPosition, dronePosition) < 10000){
			
			if(droneVulnerable == true){
				
				ChangeAntennaName(antennaShields);
				Me.CubeGrid.CustomName = shipShields;
				droneVulnerable = false;
				BlockToggle("Shield Light", true);
				Me.CustomData = droneVulnerable.ToString();
				bool result = Me.GetValue<bool>("CorruptionInvincibility");
				
			}
			
		}else{
			
			if(droneVulnerable == false){
				
				ChangeAntennaName(antennaNoShields);
				Me.CubeGrid.CustomName = shipNoShields;
				droneVulnerable = true;
				BlockToggle("Shield Light", false);
				Me.CustomData = droneVulnerable.ToString();
				bool result = Me.GetValue<bool>("CorruptionInvincibility");
				
			}
			
		}
		
	}
	
	if(currentMode == "GetCoordinator"){
		
		coordinatorId = Me.GetValue<long>("CorruptionNearestShieldDrone");
		
		if(coordinatorId != 0){
			
			currentMode = "FollowCoordinator";
			
		}else{
			
			tryGetCoordinatorAttempts++;
			
			if(tryGetCoordinatorAttempts >= 5){
				
				ChangeAntennaName(antennaNoShields);
				Me.CubeGrid.CustomName = shipNoShields;
				coordinatorId = 0;
				droneVulnerable = true;
				BlockToggle("Shield Light", false);
				Me.CustomData = droneVulnerable.ToString();
				bool result = Me.GetValue<bool>("CorruptionInvincibility");
				currentMode = "AttackPlayer";
				coordinatorProximity = false;
				
			}
			
		}
		
	}
	
	if(currentMode == "FollowCoordinator"){
		
		if(MeasureDistance(coordinatorPosition, closestPlayer) < 7000){
			
			currentMode = "AttackPlayer";
			
		}
		
		if(MeasureDistance(coordinatorPosition, dronePosition) < 350){
			
			coordinatorProximity = false;
			
		}else{
			
			coordinatorProximity = true;
			
		}
		
	}
	
	if(currentMode == "AttackPlayer"){
		
		if(MeasureDistance(coordinatorPosition, closestPlayer) > 7000){
			
			currentMode = "FollowCoordinator";
			
		}
		
		if(MeasureDistance(coordinatorPosition, dronePosition) > 350){
			
			coordinatorProximity = false;
			
		}else{
			
			coordinatorProximity = true;
			
		}
		
		var scannedData = CameraRayCast(remoteControl.WorldMatrix.Forward, 2000, out cameraDamaged);
		
		if(HasRangedTarget(scannedData, false, false, 2000) == true){
			
			currentTarget = (Vector3D)scannedData.HitPosition;
			
		}else{
			
			currentTarget = closestPlayer;
			
		}
			
		if(inNaturalGravity == false){
			
			if(MeasureDistance(dronePosition, currentTarget) >= 300){
				
				
				SetDestination(closestPlayer, coordinatorProximity, 100);			
				
			}else{
				
				SetDestination(closestPlayer, true, 20);
				
			}
			
			if(HasRangedTarget(scannedData, true, true, 800) == true || ApproximateTarget(closestPlayer, remoteControl.WorldMatrix.Forward, 10) == true){
				
				WeaponActivation("Shoot_On");
				
			}else{
				
				WeaponActivation("Shoot_Off");
				
			}
			
		}else{
			
			if(distanceDroneToPlayer > 800){
							
				SetDestination(closestPlayer, coordinatorProximity, 100);
				
			}else{
				
				timeOut++;
				double altitude = 0;
				bool elevation = remoteControl.TryGetPlanetElevation(MyPlanetElevation.Surface, out altitude);
							
				if(altitude > 400){
					
					remoteControl.SetAutoPilotEnabled(false);
					
					if(timeOut >= 5){
						
						timeOut = 0;
						if(rotateToggle == true){
							
							rotateToggle = false;
							
						}else{
							
							rotateToggle = true;
							
						}
						
						if(rotateToggle == true){
							
							AutoRotateActivation(true);
							
						}else{
							
							AutoRotateActivation(false);
							
						}
						
					}
					
					
				}else{
					
					AutoRotateActivation(false);
					SetDestination(CreateDirectionAndTarget(planetLocation, closestPlayer, closestPlayer, 800), coordinatorProximity, 100);
					
				}
				
			}
			
			if(HasRangedTarget(scannedData, true, true, 800) == true || ApproximateTarget(closestPlayer, remoteControl.WorldMatrix.Forward, 10) == true){
				
				WeaponActivation("Shoot_On");
				
			}else{
				
				WeaponActivation("Shoot_Off");
				
			}
			
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
	
	TryRotate();
	
	tickCounter += tickIncrement;
	
	if(tickCounter < 60){
		
		return;
		
	}
	
	tickCounter = 0;
	
	//Reset Block List and Check For Remote Control
	remoteControl = GridTerminalSystem.GetBlockWithName("Remote Control") as IMyRemoteControl;
	inNaturalGravity = remoteControl.TryGetPlanetPosition(out planetLocation);
	
	ResetBlockList();
	
	
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
		
		if(remoteControl == null){
		
			return;
		
		}
		
		double yaw = 0;
		double pitch = 0;
		var targetDir = Vector3D.Normalize(remoteControl.GetPosition() - closestPlayer);
		GetRotationAngles(targetDir * -1, (IMyTerminalBlock)remoteControl, out yaw, out pitch);
		ApplyGyroOverride(pitch, yaw, 0, gyroList, (IMyTerminalBlock)remoteControl);
		
	}	
	
	
	
}

Vector3D CreateDirectionAndTarget(Vector3D startDirCoords, Vector3D endDirCoords, Vector3D startPathCoords, double pathDistance){
	
	var direction = Vector3D.Normalize(endDirCoords - startDirCoords);
	var coords = direction * pathDistance + startPathCoords;
	return coords;
	
}

bool HasRangedTarget(MyDetectedEntityInfo scannedData, bool targetIsGrid, bool targetIsHostile, double maximumRange){
	
	if(scannedData.IsEmpty() == true){
		
		return false;
		
	}
	
	if(scannedData.HitPosition == null){
		
		return false;
		
	}
	
	if(MeasureDistance(dronePosition, (Vector3D)scannedData.HitPosition) > maximumRange){
		
		return false;
		
	}
	
	if(targetIsGrid == true && scannedData.Type.ToString().Contains("Grid") == false){
		
		return false;
		
	}
	
	if(scannedData.Relationship != MyRelationsBetweenPlayerAndBlock.Enemies && targetIsHostile == true){
		
		return false;
		
	}
	
	return true;
	
}

bool ApproximateTarget(Vector3D targetCoords, Vector3D myDirection, double maximumDistFromTarget){
	
	double distanceToTarget = MeasureDistance(targetCoords, dronePosition);
	var impactPosition = distanceToTarget * myDirection + dronePosition;
	
	if(distanceToTarget > 850){
		
		return false;
		
	}
	
	if(MeasureDistance(impactPosition, targetCoords) < maximumDistFromTarget){
		
		return true;
		
	}
	
	return false;
	
}

void ChangeAntennaName(string newName){
	
	foreach(var block in blockList){
		
		var antenna = block as IMyRadioAntenna;
		
		if(antenna != null){
			
			antenna.CustomName = newName;
			
		}
		
	}
	
}

void BlockToggle(string blockName, bool toggle){
	
	foreach(var block in blockList){
		
		var fBlock = block as IMyFunctionalBlock;
		
		if(fBlock == null){
			
			continue;
			
		}
		
		if(fBlock.CustomName == blockName){
			
			fBlock.Enabled = toggle;
			
		}
		
	}
	
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