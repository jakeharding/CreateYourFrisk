timer = 0
timer2 = 0
Encounter.SetVar("wavetimer",math.huge)
Arena.Resize(200,200)
Encounter.Call("Animate",Encounter['Anim_To_Side'])
Player.MoveTo(0,0,false)
difficulty = Encounter["difficulty"]

fadintime = 60
fadinstate = 0
willshoot = false
source_x = Arena.width/2 + 10
source_y = 0

side = 1

spawntimer = 20 - difficulty * 4
spawntimer2 = 200 - difficulty * 20
speed = 1.5 + difficulty * 0.2
speed2 = 1

bullets = {}

phase = 0

function Update()
	timer = timer + Time.mult
	while timer >= 1 do
		timer = timer - 1
		timer2 = timer2 + 1
		if phase == 0 then
			FadeHands()
			if willshoot == true then
				ShootBullets()
			end
			if timer2 >= 6*60 and phase == 0 then -- SET TO 12!!!
				phase = 1
				hand_targety = hand_dir * (Arena.height/2 - 5)
				ResizeCustomSpeed(2,{50,200})
				hand_to = "start"
				Encounter.Call("Animate",Encounter['Anim_To_Firewall_Mid'])
			end
		end
		if phase == 0 then
			l_hand.MoveTo(-(Arena.currentwidth/2 + 25),-20)
			r_hand.MoveTo((Arena.currentwidth/2 + 25),-20)
		end
		if phase == 1 then
			HandAttack()
			ShootBullets2()
		end
		if phase == 1.5 then
			HandAttack()
		end
		if phase == 2 then
			ShootBullets()
			if timer2 >= 6*60 and phase == 2 then
				Encounter.Call("Animate",Encounter['Anim_To_Side_Inv'])
				EndWave()
			end
		end
		MoveBullets()
		CustomResize()
		Encounter['Flame_mid_y'] = 300 + Arena.currentheight - 130
	end
end

l_hand = CreateProjectile("lhand",-(Arena.width/2 + 25),-20)
r_hand = CreateProjectile("rhand",(Arena.width/2 + 25),-20)
l_hand.sprite.alpha = 0
r_hand.sprite.alpha = 0

hand_flames = {}

for i = 1,6 do
	local bullet = CreateProjectile("fball",-35 + i * 10,Arena.height/2 - 5)
	bullet.SetVar('type',"handflame")
	bullet.sprite.alpha = 0
	table.insert(hand_flames,bullet)
end

function FadeHands()
	if fadinstate < fadintime then
		fadinstate = fadinstate + 1
		l_hand.sprite.alpha = fadinstate/fadintime
		r_hand.sprite.alpha = fadinstate/fadintime
	elseif willshoot ~= true then
		willshoot = true
	end
end

function MoveHands(y)
	l_hand.MoveTo(-(Arena.currentwidth/2 + 25),y - 20)
	r_hand.MoveTo((Arena.currentwidth/2 + 25),y - 20)
	for i = 1,#hand_flames do
		local flame = hand_flames[i]
		flame.MoveTo(flame.x,y)
	end
end

hand_dir = (math.random(1,2)-1.5)*2
hand_speed = 0
hand_targety = 0
hand_y = 0
flames_on = false
newsize = {40,200}
hand_to = "none" -- possible: "start" = no flames yet; "end" = flames on; "none" = no acceleration

hand_attack = 0

acc_start = 0.3
acc_end = 0.05
function SetMaxSpeed()
	max_speed_start = 4 + hand_attack/2
	max_speed_end = 1.5 + hand_attack/10
end

SetMaxSpeed()

function HandAttack()
	hand_y = l_hand.y + 20
	hand_dist = hand_targety - hand_y
	hand_go = hand_dist/math.abs(hand_dist)
	if math.abs(hand_dist) - math.abs(hand_speed) < 0 then
		MoveHands(hand_targety)
		if hand_to == "start" and phase == 1 then
			Audio.PlaySound("bfball")
			hand_to = "end"
			for i = 1,#hand_flames do
				local flame = hand_flames[i]
				flame.sprite.alpha = 1
			end
			hand_targety = - hand_dir * (Arena.height/2 - 35 + difficulty * 5)
		elseif hand_to == "end" then
			ResizeCustomSpeed(0.2,{50,Arena.height-6})
			hand_to = "start"
			hand_dir = (math.random(1,2)-1.5)*2
			hand_targety = hand_dir * (Arena.height/2 + 5)
			hand_attack = hand_attack + 1
			SetMaxSpeed()
			if hand_attack > 3 * difficulty + 6 then
				phase = 1.5
				Encounter.Call("Animate",Encounter['Anim_From_Firewall_Mid'])
				hand_targety = 0
				hand_to = "none"
				ResizeCustomSpeed(5,{200,200})
			end
			for i = 1,#hand_flames do
				local flame = hand_flames[i]
				flame.sprite.alpha = 0
			end
		end
		if hand_to == "none" and phase == 1.5 and resize == false then
			phase = 2
			timer2 = 0
		end
		if phase ~= 1.5 then
			hand_speed = 0
		end
	else
		MoveHands(hand_y + hand_go * hand_speed)
		if hand_to == "start" then
			if hand_speed + acc_start < max_speed_start then
				hand_speed = hand_speed + acc_start
			else
				hand_speed = max_speed_start
			end
		elseif hand_to == "end" or hand_to == "none" then
			if hand_speed + acc_end < max_speed_end then
				hand_speed = hand_speed + acc_end
			else
				hand_speed = max_speed_end
			end
		end
	end
end

function MoveBullets()
	for i = 1,#bullets do
		local bullet = bullets[i]
		if bullet.isactive then
			local velx = bullet.GetVar('velx')
			local vely = bullet.GetVar('vely')
			local posx = bullet.GetVar('posx')
			local posy = bullet.GetVar('posy')
			if (math.abs(posx + velx) > Arena.width/2 or
			math.abs(posy + vely) > Arena.height/2) and
			math.abs(bullet.x) < Arena.width/2 and
			math.abs(bullet.y) < Arena.height/2 then
				bullet.Remove()
			else
				local posx = posx + velx
				local posy = posy + vely
				bullet.MoveTo(posx,posy)
				bullet.SetVar('posx',posx)
				bullet.SetVar('posy',posy)
			end
		end
	end
end

function ShootBullets()
	if timer2%spawntimer == 0 then
		side = -side
		Audio.PlaySound("fball")
		for i = -1,1 do
			local bullet = CreateProjectile("fball",side * source_x,source_y)
			local dir = Direction({Player.x,Player.y + i*20},{bullet.x,bullet.y})
			bullet.SetVar('velx',speed * dir[1])
			bullet.SetVar('vely',speed * dir[2])
			bullet.SetVar('posx',bullet.x)
			bullet.SetVar('posy',bullet.y)
			table.insert(bullets,bullet)
		end
	end
end

function ShootBullets2()
	if timer2%spawntimer2 == 0 then
		Audio.PlaySound("fball")
		side = (math.random(1,2)-1.5)*2
		local bullet = CreateProjectile("fball",math.random(40)-20,side * (Arena.height/2 + 50))
		bullet.SetVar('velx',0)
		bullet.SetVar('vely',speed2 * -side)
		bullet.SetVar('posx',bullet.x)
		bullet.SetVar('posy',bullet.y)
		table.insert(bullets,bullet)
	end
end

function Direction(postarget,posshooter)
	local dx = postarget[1] - posshooter[1]
	local dy = postarget[2] - posshooter[2]
	local d = math.sqrt(dx^2+dy^2)
	local alpha = math.acos(dx/d)
	if dy > 0 then
		alpha = alpha
	else
		alpha = - alpha
	end
	local x = math.cos(alpha)
	local y = math.sin(alpha)
	local dir = {x,y}
	return dir
end

function OnHit(bullet)
	if bullet.GetVar('type') == "handflame" and hand_to ~= "end" then
	else
		Player.Hurt(5)
	end
end

resize_speed = 0
resize_target = {}
resize = false

function ResizeCustomSpeed(speed,target)
	resize_speed = speed
	resize_target = target
	resize = true
end

function CustomResize()
	if resize == true then
		local dw = resize_target[1] - Arena.width
		local w = dw/math.abs(dw)
		local dh = resize_target[2] - Arena.height
		local h = dh/math.abs(dh)
		local neow = 0
		local neoh = 0
		if math.abs(dw) - resize_speed > 0 then
			neow = Arena.width + w * resize_speed
		else
			neow = resize_target[1]
		end
		if math.abs(dh) - resize_speed > 0 then
			neoh = Arena.height + h * resize_speed
		else
			neoh = resize_target[2]
		end
		Arena.ResizeImmediate(neow,neoh)
		if neow == resize_target[1] and neoh == resize_target[2] then
			resize = false
		end
	end
end