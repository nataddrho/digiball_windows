import random

f = open("display.htm","w")

angle = random.randint(0,360)
tipPercent = random.randint(0,60)

f.write("""

<!DOCTYPE html>
<html>	
	<body>
		<iframe style="border: none; position: fixed; left: 0; right: 0; top: 0; bottom: 0; width: 100%; height: 100%;" 
""")

f.write('src="_digiball_insert.htm?angle=%i&tipPercent=%i"></iframe>'%(angle,tipPercent))

f.write("""

	</body>
</html>


""")

f.close()


