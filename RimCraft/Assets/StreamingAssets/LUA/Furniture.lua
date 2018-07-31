function OnUpdate_GasGenerator(furniture, deltaTime)
	if (furniture.tile.room == nil) then
		return "Error: room of furniture is null."
	end
	
	if (furniture.tile.room.GetGasAmount("O2") < 0.20) then
		furniture.tile.room.ChangeGas("O2", 0.01 * deltaTime)
	else
		-- Что то нужно предпринять если давление достигло предела. Мб выключиться
	end
end

function OnUpdate_Door( furniture, deltaTime)
	local openSpeed = 4;
	
	if (furniture.GetParameter("is_opening") >= 1) then
		furniture.ChangeParameter("openness", deltaTime * openSpeed)
		if (furniture.GetParameter("openness") >= 1) then
			furniture.SetParameter("is_opening",  0)
		end
	else
		furniture.ChangeParameter("openness", -deltaTime * openSpeed)
	end
	furniture.SetParameter("openness", Clamp01(furniture.GetParameter("openness")))

	if (furniture.cbOnChanged != nil) then
		furniture.cbOnChanged(furniture)
	end
end

function Clamp01(value)
	if (value > 1) then
		return 1
	elseif (value < 0) then
		return 0
	end
	
	return value;
end

-- return "LUA Script parset"