//Swift Drone AI Script

//Configuration
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
bool rotateToggle = false;

//Global Blocks List & Remote Control
List<IMyTerminalBlock> blockList = new List<IMyTerminalBlock>();
List<IMyGyro> gyroList = new List<IMyGyro>();
IMyRemoteControl remoteControl;
IMyCameraBlock forwardCamera;

string currentMode = "Seeking";
int gravityDriveTimer = 0;
int gravityCoolDown = 0;
int gravityDriveTimerMax = 5;
int gravityCoolDownMax = 2;
int timeOut = 0;
bool autoRotate = false;
bool altitudeAdjust = false;
int tickIncrement = 10;
int tickCounter = 0;
Random rnd = new Random();
Vector3D escapeDirection = new Vector3D(0,0,0);

public Program(){
	
	Runtime.UpdateFrequency = UpdateFrequency.Update10;
	
}


void DroneBehaviour(string argument){
	
	TryIceRefill();
	var forwardTarget = new MyDetectedEntityInfo();
	
	if(forwardCamera == null){
		
		forwardCamera = GridTerminalSystem.GetBlockWithName("Camera (Forward)") as IMyCameraBlock;
		
	}
	
	if(forwardCamera != null){
		
		forwardCamera.EnableRaycast = true;
		
		if(forwardCamera.CanScan(800) == true){
			
			forwardTarget = forwardCamera.Raycast(800,0,0);
			
		}
		
	}
	
	if(forwardTarget.IsEmpty() == false && forwardCamera != null){
			
		if(forwardTarget.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies){
			
			WeaponActivation("Shoot_On");
			
		}else{
			
			WeaponActivation("Shoot_Off");
			
		}
		
	}else{
		
		if(ApproximateTarget(remoteControl.GetPosition(), closestPlayer, remoteControl.WorldMatrix.Forward, 10) == true && distanceDroneToPlayer < 800){
			
			WeaponActivation("Shoot_On");
			
		}else{
			
			WeaponActivation("Shoot_Off");
			
		}

	}
	
	Echo(currentMode);
	Echo(gravityCoolDown.ToString());
	
	if(inNaturalGravity == true){
		
		double altitude = 0;
		remoteControl.TryGetPlanetElevation(MyPlanetElevation.Surface, out altitude);
		
		if(altitude != 0 && altitude < 750){
			
			currentMode = "SurfaceEscapeManuever";
			
		}
		
	}
	
	if(currentMode == "Seeking"){
		
		bool targetFacingDroneAtRange = false;
		var detectedTarget = new MyDetectedEntityInfo();
		
		if(distanceDroneToPlayer < 1300){
			
			targetFacingDroneAtRange = TargetDetection(out detectedTarget);
			
		}else{
			
			targetFacingDroneAtRange = false;
			
		}
		
		if(gravityCoolDown < 2){
			
			gravityCoolDown++;
			
		}
				
		if(targetFacingDroneAtRange == true && gravityCoolDown >= 2){
			
			gravityCoolDown = 0;
			gravityDriveTimer = 0;
			remoteControl.SetAutoPilotEnabled(false);
			remoteControl.DampenersOverride = false;
			AutoRotateActivation(true);
			
			var randomDirListAll = new List<Vector3D>();		
			randomDirListAll.Add(remoteControl.WorldMatrix.Right);
			randomDirListAll.Add(remoteControl.WorldMatrix.Left);
			randomDirListAll.Add(remoteControl.WorldMatrix.Up);
			randomDirListAll.Add(remoteControl.WorldMatrix.Down);
			randomDirListAll.Add(remoteControl.WorldMatrix.Forward);
			randomDirListAll.Add(remoteControl.WorldMatrix.Backward);
			
			var closestDir = new Vector3D(0,0,0);
			
			if(MeasureDistance(detectedTarget.Position, dronePosition) < 500){
				
				foreach(var dir in randomDirListAll){
					
					if(closestDir == new Vector3D(0,0,0)){
						
						closestDir = dir;
						continue;
						
					}
					
					var previousDist = MeasureDistance(closestDir, closestPlayer);
					var currentDist = MeasureDistance(dir, closestPlayer);
					
					if(currentDist < previousDist){
						
						closestDir = dir;
						
					}
										
				}
				
				randomDirListAll.Remove(closestDir);
				randomDirListAll.Remove(closestDir * -1);
				
			}
			
			if(inNaturalGravity == true){

				closestDir = new Vector3D(0,0,0);
				
				foreach(var dir in randomDirListAll){
					
					if(closestDir == new Vector3D(0,0,0)){
						
						closestDir = dir;
						continue;
						
					}
					
					var previousDist = MeasureDistance(closestDir, planetLocation);
					var currentDist = MeasureDistance(dir, planetLocation);
					
					if(currentDist < previousDist){
						
						closestDir = dir;
						
					}
					
				}
				
				randomDirListAll.Remove(closestDir);

			}
			
			var selectedRandomDir = randomDirListAll[rnd.Next(0, randomDirListAll.Count)];
			
			OverrideThrust(false, selectedRandomDir, 0);
			OverrideThrust(true, selectedRandomDir, 100);
			var thrustDictionary = GetThrustDirections((IMyTerminalBlock)remoteControl);
			GravityDriveActivation((IMyTerminalBlock)remoteControl, thrustDictionary, true);
			
			currentMode = "GravityManuever";
			return;
			
		}
		
		if(inNaturalGravity == false){
			
			var targetCoords = CreateDirectionAndTarget(closestPlayer, dronePosition, closestPlayer, 300);
			SetDestination(targetCoords, false, 100);
			
		}else{
			
			var targetCoords = CreateDirectionAndTarget(planetLocation, closestPlayer, closestPlayer, 300);
			SetDestination(targetCoords, false, 100);
			
		}
		
		if(distanceDroneToPlayer < 300){
				
			remoteControl.SetAutoPilotEnabled(false);
			remoteControl.DampenersOverride = true;
				
		}
				
	}
	
	if(currentMode == "GravityManuever"){
		
		var thrustDictionary = GetThrustDirections((IMyTerminalBlock)remoteControl);
		GravityDriveActivation((IMyTerminalBlock)remoteControl, thrustDictionary, true);
		gravityDriveTimer++;
		
		if(gravityDriveTimer >= gravityDriveTimerMax - 1){
			
			OverrideThrust(false, new Vector3D(0,0,0), 0);
			remoteControl.DampenersOverride = true;
			
		}
		
		if(gravityDriveTimer >= 5){
			
			AutoRotateActivation(false);
			thrustDictionary = GetThrustDirections((IMyTerminalBlock)remoteControl);
			GravityDriveActivation((IMyTerminalBlock)remoteControl, thrustDictionary, false);
			OverrideThrust(false, new Vector3D(0,0,0), 0);
			currentMode = "Seeking";
			
		}
		
	}
	
	if(currentMode == "SurfaceEscapeManuever"){
		
		if(inNaturalGravity == false){
			
			//Set Mode Back To Seeking
			currentMode = "Seeking";
			AutoRotateActivation(false);
			var thrustDictionary = GetThrustDirections((IMyTerminalBlock)remoteControl);
			GravityDriveActivation((IMyTerminalBlock)remoteControl, thrustDictionary, false);
			OverrideThrust(false, new Vector3D(0,0,0), 0);
			return;
			
		}
		
		double altitude = 0;
		remoteControl.TryGetPlanetElevation(MyPlanetElevation.Surface, out altitude);
		if(altitude != 0 && altitude > 750){
			
			//Set Mode Back To Seeking
			currentMode = "Seeking";
			AutoRotateActivation(false);
			var thrustDictionary = GetThrustDirections((IMyTerminalBlock)remoteControl);
			GravityDriveActivation((IMyTerminalBlock)remoteControl, thrustDictionary, false);
			OverrideThrust(false, new Vector3D(0,0,0), 0);
			return;
			
		}
		
		var escapeCoords = Vector3D.Normalize(dronePosition - planetLocation) * 1000 + dronePosition;
		escapeDirection = Vector3D.Normalize(escapeCoords - dronePosition);
		
		var randomDirListAll = new List<Vector3D>();		
		randomDirListAll.Add(remoteControl.WorldMatrix.Right);
		randomDirListAll.Add(remoteControl.WorldMatrix.Left);
		randomDirListAll.Add(remoteControl.WorldMatrix.Up);
		randomDirListAll.Add(remoteControl.WorldMatrix.Down);
		randomDirListAll.Add(remoteControl.WorldMatrix.Backward);
		
		bool notFurthest = false;
		
		foreach(var direction in randomDirListAll){
			
			if(MeasureDistance(direction, escapeDirection) < MeasureDistance(remoteControl.WorldMatrix.Forward, escapeDirection)){
				
				notFurthest = true;
				break;
				
			}
			
		}
		
		AutoRotateActivation(true);
		
		if(notFurthest == false){
			
			OverrideThrust(true, remoteControl.WorldMatrix.Forward, 100);
			var thrustDictionary = GetThrustDirections((IMyTerminalBlock)remoteControl);
			GravityDriveActivation((IMyTerminalBlock)remoteControl, thrustDictionary, true);
			
		}else{
			
			OverrideThrust(false, new Vector3D(0,0,0), 0);
			var thrustDictionary = GetThrustDirections((IMyTerminalBlock)remoteControl);
			GravityDriveActivation((IMyTerminalBlock)remoteControl, thrustDictionary, true);
			
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

bool TargetDetection(out MyDetectedEntityInfo scannedEnemyData){
	
	scannedEnemyData = new MyDetectedEntityInfo();
	
	foreach(var block in blockList){
		
		var camera = block as IMyCameraBlock;
		var turret = block as IMyLargeTurretBase;
		
		if(camera != null && block.IsFunctional == true){
			
			camera.EnableRaycast = true;
			
			if(camera.CanScan(MeasureDistance(dronePosition, closestPlayer)) == true && MeasureDistance(dronePosition, closestPlayer) < 2000){
				
				var scannedData = camera.Raycast(closestPlayer);
				
				if(scannedData.IsEmpty() == true){
					
					continue;
					
				}
				
				if(scannedData.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies){
					
					scannedEnemyData = scannedData;
					break;
					
				}
				
				
			}
			
		}
		
		if(turret != null && block.IsFunctional == true){
			
			var scannedData = turret.GetTargetedEntity();
				
			if(scannedData.IsEmpty() == true){
				
				continue;
				
			} 
			
			if(scannedData.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies){
					
				scannedEnemyData = scannedData;
				break;
				
			}
			
		}
		
	}
	
	
	
	if(scannedEnemyData.IsEmpty() == true){
		
		return false;
		
	}
	
	var enemyPosition = scannedEnemyData.Position;
	var enemyMatrix = scannedEnemyData.Orientation;
	
	if(ApproximateTarget(enemyPosition, closestPlayer, enemyMatrix.Forward, 25) == true){
		
		return true;
		
	}
	
	if(ApproximateTarget(enemyPosition, closestPlayer, enemyMatrix.Backward, 25) == true){
		
		return true;
		
	}
	
	if(ApproximateTarget(enemyPosition, closestPlayer, enemyMatrix.Left, 25) == true){
		
		return true;
		
	}
	
	if(ApproximateTarget(enemyPosition, closestPlayer, enemyMatrix.Right, 25) == true){
		
		return true;
		
	}
	
	if(ApproximateTarget(enemyPosition, closestPlayer, enemyMatrix.Up, 25) == true){
		
		return true;
		
	}
	
	if(ApproximateTarget(enemyPosition, closestPlayer, enemyMatrix.Down, 25) == true){
		
		return true;
		
	}
	
	return false;
	
}

void WeaponActivation(string weaponMode){
	
	foreach(var block in blockList){
		
		var weapon = block as IMyUserControllableGun;
		var turret = block as IMyLargeTurretBase;
		
		if(weapon == null || block.IsFunctional == false || turret != null){
			
			continue;
			
		}
		
		weapon.ApplyAction(weaponMode);
		
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
		
		if(remoteControl == null){
		
			return;
		
		}
		
		double yaw = 0;
		double pitch = 0;
		var targetDir = Vector3D.Normalize(remoteControl.GetPosition() - closestPlayer);
		
		if(currentMode == "SurfaceEscapeManuever"){
			
			targetDir = escapeDirection * -1;
			
		}
		
		GetRotationAngles(targetDir * -1, (IMyTerminalBlock)remoteControl, out yaw, out pitch);
		ApplyGyroOverride(pitch, yaw, 0, gyroList, (IMyTerminalBlock)remoteControl);
		
	}	
	
	
	
}

Vector3D CreateDirectionAndTarget(Vector3D startDirCoords, Vector3D endDirCoords, Vector3D startPathCoords, double pathDistance){
	
	var direction = Vector3D.Normalize(endDirCoords - startDirCoords);
	var coords = direction * pathDistance + startPathCoords;
	return coords;
	
}

bool ApproximateTarget(Vector3D startCoords, Vector3D targetCoords, Vector3D myDirection, double maximumDistFromTarget){
	
	double distanceToTarget = MeasureDistance(targetCoords, startCoords);
	Vector3D impactPosition = distanceToTarget * myDirection + startCoords;
	
	if(distanceToTarget > 850){
		
		return false;
		
	}
	
	if(MeasureDistance(impactPosition, targetCoords) < maximumDistFromTarget){
		
		return true;
		
	}
	
	return false;
	
}

void OverrideThrust(bool enableOverride, Vector3D direction, float thrustModifier){
	
	foreach(var block in blockList){
		
		var thruster = block as IMyThrust;
		
		if(thruster == null || block.IsFunctional == false){
			
			continue;
			
		}
		
		if(enableOverride == true){
			
			if(thruster.WorldMatrix.Forward == direction * -1){
			
				float maxthrust = thruster.MaxThrust;
				thruster.ThrustOverridePercentage = thrustModifier;
			
			}
			
		}else{
			
			thruster.SetValueFloat("Override", 0);
			
		}
		
	}
	
}

Dictionary<string, string> GetThrustDirections(IMyTerminalBlock referenceBlock){
	
	var thrustDirections = new Dictionary<string, string>();
	
	foreach(var block in blockList){
		
		if(block == null){
			
			continue;
			
		}
		
		var thrust = block as IMyThrust;
		
		if(thrust == null || block.IsFunctional == false){
			
			continue;
			
		}
		
		if(thrust.CurrentThrust == thrust.MaxThrust){
			
			//Up
			if(thrust.WorldMatrix.Backward == referenceBlock.WorldMatrix.Up){
				
				if(thrustDirections.ContainsKey("Y") == false){
					
					thrustDirections.Add("Y", "Up");
					
				}
				
			}
			
			//Down
			if(thrust.WorldMatrix.Backward == referenceBlock.WorldMatrix.Down){
				
				if(thrustDirections.ContainsKey("Y") == false){
					
					thrustDirections.Add("Y", "Down");
					
				}
				
			}
			
			//Left
			if(thrust.WorldMatrix.Backward == referenceBlock.WorldMatrix.Left){
				
				if(thrustDirections.ContainsKey("X") == false){
					
					thrustDirections.Add("X", "Left");
					
				}
				
			}
			
			//Right
			if(thrust.WorldMatrix.Backward == referenceBlock.WorldMatrix.Right){
				
				if(thrustDirections.ContainsKey("X") == false){
					
					thrustDirections.Add("X", "Right");
					
				}
				
			}
			
			//Forward
			if(thrust.WorldMatrix.Backward == referenceBlock.WorldMatrix.Forward){
				
				if(thrustDirections.ContainsKey("Z") == false){
					
					thrustDirections.Add("Z", "Forward");
					
				}
				
			}
			
			//Backward
			if(thrust.WorldMatrix.Backward == referenceBlock.WorldMatrix.Backward){
				
				if(thrustDirections.ContainsKey("Z") == false){
					
					thrustDirections.Add("Z", "Backward");
					
				}
				
			}
			
		}
		
	}
	
	return thrustDirections;
	
}

void GravityDriveActivation(IMyTerminalBlock referenceBlock, Dictionary<string, string> directions, bool driveEnabled){
	
	foreach(var block in blockList){
		
		if(block == null){
			
			continue;
			
		}
		
		var mass = block as IMyArtificialMassBlock;
		var gravity = block as IMyGravityGenerator;
		
		if(mass != null && block.IsFunctional == true){
			
			mass.Enabled = driveEnabled;
			
		}
		
		if(gravity != null && block.IsFunctional == true){
			
			if(driveEnabled == false){
				
				gravity.Enabled = driveEnabled;
				continue;
				
			}
			
			//X
			if(gravity.WorldMatrix.Down == referenceBlock.WorldMatrix.Left || gravity.WorldMatrix.Down == referenceBlock.WorldMatrix.Right){
				
				if(directions.ContainsKey("X") == true){
					
					gravity.Enabled = true;
					
					if(directions["X"] == "Left"){
						
						if(gravity.WorldMatrix.Down == referenceBlock.WorldMatrix.Left){
							
							gravity.GravityAcceleration = 10;
							
						}else{
							
							gravity.GravityAcceleration = -10;
							
						}
						
					}else{
						
						if(gravity.WorldMatrix.Down == referenceBlock.WorldMatrix.Right){
							
							gravity.GravityAcceleration = 10;
							
						}else{
							
							gravity.GravityAcceleration = -10;
							
						}
						
					}
						
					continue;
					
				}
				
			}
			
			//Y
			if(gravity.WorldMatrix.Down == referenceBlock.WorldMatrix.Up || gravity.WorldMatrix.Down == referenceBlock.WorldMatrix.Down){
				
				if(directions.ContainsKey("Y") == true){
					
					gravity.Enabled = true;
					
					if(directions["Y"] == "Up"){
						
						if(gravity.WorldMatrix.Down == referenceBlock.WorldMatrix.Up){
							
							gravity.GravityAcceleration = 10;
							
						}else{
							
							gravity.GravityAcceleration = -10;
							
						}
						
					}else{
						
						if(gravity.WorldMatrix.Down == referenceBlock.WorldMatrix.Down){
							
							gravity.GravityAcceleration = 10;
							
						}else{
							
							gravity.GravityAcceleration = -10;
							
						}
						
					}
						
					continue;
					
				}
				
			}
			
			//Z
			if(gravity.WorldMatrix.Down == referenceBlock.WorldMatrix.Forward || gravity.WorldMatrix.Down == referenceBlock.WorldMatrix.Backward){
				
				if(directions.ContainsKey("Z") == true){
					
					gravity.Enabled = true;
					
					if(directions["Z"] == "Forward"){
						
						if(gravity.WorldMatrix.Down == referenceBlock.WorldMatrix.Forward){
							
							gravity.GravityAcceleration = 10;
							
						}else{
							
							gravity.GravityAcceleration = -10;
							
						}
						
					}else{
						
						if(gravity.WorldMatrix.Down == referenceBlock.WorldMatrix.Backward){
							
							gravity.GravityAcceleration = 10;
							
						}else{
							
							gravity.GravityAcceleration = -10;
							
						}
						
					}
						
					continue;
					
				}
				
			}
			
			gravity.Enabled = false;
			
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