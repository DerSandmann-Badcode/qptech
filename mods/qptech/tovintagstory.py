import c4d
from c4d import gui
from c4d import documents
#Welcome to the world of Python
objectnumber=0
radtodeg=57.2958
def test(texty):
    gui.MessageDialog(texty)

def getfromv(points):
    minx=9999
    miny=9999
    minz=9999
    maxx=-9999
    maxy=-9999
    maxz=-9999
    for p in points:
        if p.x<minx: minx=p.x
        if p.y<miny: miny=p.y
        if p.z<minz: minz=p.z
        if p.x>maxx: maxx=p.x
        if p.y>maxy: maxy=p.y
        if p.z>maxz: maxz=p.z
    retvector=c4d.Vector(minx,miny,minz)
    return retvector
def gettov(points):
    minx=9999
    miny=9999
    minz=9999
    maxx=-9999
    maxy=-9999
    maxz=-9999
    for p in points:
        if p.x<minx: minx=p.x
        if p.y<miny: miny=p.y
        if p.z<minz: minz=p.z
        if p.x>maxx: maxx=p.x
        if p.y>maxy: maxy=p.y
        if p.z>maxz: maxz=p.z
    retvector=c4d.Vector(maxx,maxy,maxz)
    return retvector
def addelement(c,parentvec):
    global objectnumber
    global radtodeg
    print (parentvec)
    json=""
    json+=' {\n'
    json+='  "name" : "%s%i",\n'%(c.GetName(),objectnumber)
    objectnumber+=1
    #find all the coordinates information (sort of a bounding box)
    startpos=c.GetAbsPos()
    startpos.x=startpos.x/10
    startpos.y=startpos.y/10
    startpos.z=startpos.z/10
    tov = c4d.Vector(0,0,0)
    fromv = c4d.Vector(0,0,0)
    points=c.GetAllPoints()
    fromv = getfromv(points)
    fromv.x=fromv.x/10+startpos.x+parentvec.x
    fromv.y=fromv.y/10+startpos.y+parentvec.y
    fromv.z=fromv.z/10+startpos.z+parentvec.z
    tov = gettov(points)
    tov.x=tov.x/10+startpos.x+parentvec.x
    tov.y=tov.y/10+startpos.y+parentvec.y
    tov.z=tov.z/10+startpos.z+parentvec.z
    #handle rotation information
    rotation=c.GetAbsRot()
    rotation.x*=-radtodeg #H or y rotation in VS
    rotation.y*=-radtodeg #P or x rotation in VS
    rotation.z*=-radtodeg #B or z rotation in VS
    origin=c4d.Vector(parentvec.x,parentvec.y,parentvec.z)
    origin.x+=startpos.x
    origin.y+=startpos.y
    origin.z+=startpos.z
    #find texture information
    tags=c.GetTags()
    texture =''

    for tag in tags:
        if type(tag)==c4d.TextureTag:
            texture=tag.GetMaterial().GetName()
            texture=texture.split("/",1)[1]
    json+=('  "from": [ %f, %f, %f ],\n'%(fromv.x,fromv.y,fromv.z))
    json+='  "to": [ %f, %f, %f ],\n'%(tov.x,tov.y,tov.z)
    json+='  "rotationOrigin": [ %f, %f, %f ],\n'%(origin.x,origin.y,origin.z)
    json+='  "rotationY": %f,\n'%rotation.x
    json+='  "rotationX": %f,\n'%rotation.y
    json+='  "rotationZ": %f,\n'%rotation.z
    json+='  "faces": {\n'
    json+='    "north": { "texture": "#%s", "uv": [ 0.0, 0.0, 16.0, 16.0 ] },\n'%texture
    json+='    "east": { "texture": "#%s", "uv": [ 0.0, 0.0, 16.0, 16.0 ] },\n'%texture
    json+='    "south": { "texture": "#%s", "uv": [ 0.0, 0.0, 16.0, 16.0 ] },\n'%texture
    json+='    "west": { "texture": "#%s", "uv": [ 0.0, 0.0, 16.0, 16.0 ] },\n'%texture
    json+='    "up": { "texture": "#%s", "uv": [ 0.0, 0.0, 16.0, 16.0 ] },\n'%texture
    json+='    "down": { "texture": "#%s", "uv": [ 0.0, 0.0, 16.0, 16.0 ] }\n'%texture
    json+='   }\n'
    #check for children
    children=c.GetChildren()
    if len(children)>0:
        json+=',"children": ['
        for child in children:
            json+=addelement(child,origin)
        json +="]"
    json+='  }'
    json+=","
    return json
def main():

    #start of json and header
    json="{\n"
    json+='"editor": {"allAngles": false,"entityTextureMode": false},\n'
    json+='"textureWidth": 16,\n"textureHeight": 16,\n"textureSizes": {},\n'

    #material definition
    json+='"textures": {\n'
    actmat = c4d.documents.GetActiveDocument().GetMaterials()
    listctr=0;
    for x in actmat:
      fullname=x.GetName()
      json += ' "'+fullname.split("/",1)[1]+'" :'+'"'+fullname+'"'
      listctr+=1
      if (listctr<len(actmat)):
          json+=',\n'
      else:
          json+='\n'
    json+='},\n'

    #elements - will have to also go thru each sub element
    json+='"elements": [\n'
    actsel = c4d.documents.GetActiveDocument().GetObjects()
    listctr=0;
    zerovec=c4d.Vector(8,8,8)
    for c in actsel:
        if c.GetName()=="scene": continue
        json+=addelement(c,zerovec)

        #setup each object as a cuboid

    json+='\n]\n'
    #end of json
    json+="}"

    #final output
    print (json);

if __name__=='__main__':
    main()