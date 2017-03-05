import pyproj
import re

s = '''1920-1	http://10.0.36.51:7878//cgi-bin/mapserv.exe?map=/ms4w/apache/htdocs/nls-seperated.map&&SERVICE=WMS&VERSION=1.1.1&REQUEST=GetMap&BBOX=589400,194000,590400,195000&SRS=EPSG:27700&WIDTH=1512&HEIGHT=1512&LAYERS=nls-cs-1920&STYLES=&FORMAT=image/png&DPI=96&MAP_RESOLUTION=96&FORMAT_OPTIONS=dpi:96&TRANSPARENT=TRUE
1920-2	http://10.0.36.51:7878//cgi-bin/mapserv.exe?map=/ms4w/apache/htdocs/nls-seperated.map&&SERVICE=WMS&VERSION=1.1.1&REQUEST=GetMap&BBOX=594332,195744,595332,196744&SRS=EPSG:27700&WIDTH=1512&HEIGHT=1512&LAYERS=nls-cs-1920&STYLES=&FORMAT=image/png&DPI=96&MAP_RESOLUTION=96&FORMAT_OPTIONS=dpi:96&TRANSPARENT=TRUE
1920-3	http://10.0.36.51:7878//cgi-bin/mapserv.exe?map=/ms4w/apache/htdocs/nls-seperated.map&&SERVICE=WMS&VERSION=1.1.1&REQUEST=GetMap&BBOX=595270,199244,596270,200244&SRS=EPSG:27700&WIDTH=1512&HEIGHT=1512&LAYERS=nls-cs-1920&STYLES=&FORMAT=image/png&DPI=96&MAP_RESOLUTION=96&FORMAT_OPTIONS=dpi:96&TRANSPARENT=TRUE
1920-4	http://10.0.36.51:7878//cgi-bin/mapserv.exe?map=/ms4w/apache/htdocs/nls-seperated.map&&SERVICE=WMS&VERSION=1.1.1&REQUEST=GetMap&BBOX=591252,201035,592252,202035&SRS=EPSG:27700&WIDTH=1512&HEIGHT=1512&LAYERS=nls-cs-1920&STYLES=&FORMAT=image/png&DPI=96&MAP_RESOLUTION=96&FORMAT_OPTIONS=dpi:96&TRANSPARENT=TRUE
1920-5	http://10.0.36.51:7878//cgi-bin/mapserv.exe?map=/ms4w/apache/htdocs/nls-seperated.map&&SERVICE=WMS&VERSION=1.1.1&REQUEST=GetMap&BBOX=583951,206354,584951,207354&SRS=EPSG:27700&WIDTH=1512&HEIGHT=1512&LAYERS=nls-cs-1920&STYLES=&FORMAT=image/png&DPI=96&MAP_RESOLUTION=96&FORMAT_OPTIONS=dpi:96&TRANSPARENT=TRUE
1920-6	http://10.0.36.51:7878//cgi-bin/mapserv.exe?map=/ms4w/apache/htdocs/nls-seperated.map&&SERVICE=WMS&VERSION=1.1.1&REQUEST=GetMap&BBOX=589969,208206,590969,209206&SRS=EPSG:27700&WIDTH=1512&HEIGHT=1512&LAYERS=nls-cs-1920&STYLES=&FORMAT=image/png&DPI=96&MAP_RESOLUTION=96&FORMAT_OPTIONS=dpi:96&TRANSPARENT=TRUE
1920-7	http://10.0.36.51:7878//cgi-bin/mapserv.exe?map=/ms4w/apache/htdocs/nls-seperated.map&&SERVICE=WMS&VERSION=1.1.1&REQUEST=GetMap&BBOX=594993,209997,595993,210997&SRS=EPSG:27700&WIDTH=1512&HEIGHT=1512&LAYERS=nls-cs-1920&STYLES=&FORMAT=image/png&DPI=96&MAP_RESOLUTION=96&FORMAT_OPTIONS=dpi:96&TRANSPARENT=TRUE
1920-8	http://10.0.36.51:7878//cgi-bin/mapserv.exe?map=/ms4w/apache/htdocs/nls-seperated.map&&SERVICE=WMS&VERSION=1.1.1&REQUEST=GetMap&BBOX=596332,210189,597332,211189&SRS=EPSG:27700&WIDTH=1512&HEIGHT=1512&LAYERS=nls-cs-1920&STYLES=&FORMAT=image/png&DPI=96&MAP_RESOLUTION=96&FORMAT_OPTIONS=dpi:96&TRANSPARENT=TRUE
1920-9	http://10.0.36.51:7878//cgi-bin/mapserv.exe?map=/ms4w/apache/htdocs/nls-seperated.map&&SERVICE=WMS&VERSION=1.1.1&REQUEST=GetMap&BBOX=600300,211860,601300,212860&SRS=EPSG:27700&WIDTH=1512&HEIGHT=1512&LAYERS=nls-cs-1920&STYLES=&FORMAT=image/png&DPI=96&MAP_RESOLUTION=96&FORMAT_OPTIONS=dpi:96&TRANSPARENT=TRUE
1920-10	http://10.0.36.51:7878//cgi-bin/mapserv.exe?map=/ms4w/apache/htdocs/nls-seperated.map&&SERVICE=WMS&VERSION=1.1.1&REQUEST=GetMap&BBOX=624502,221347,625502,222347&SRS=EPSG:27700&WIDTH=1512&HEIGHT=1512&LAYERS=nls-cs-1920&STYLES=&FORMAT=image/png&DPI=96&MAP_RESOLUTION=96&FORMAT_OPTIONS=dpi:96&TRANSPARENT=TRUE'''


l = re.split('&BBOX=|&SRS', s)

british = pyproj.Proj("+init=EPSG:27700")
wgs84 = pyproj.Proj("+init=EPSG:4326")


for i in range(0,len(l)):
    if(i%2==1):
        #print(l[i])
        coordinates = l[i].split(',')
        lon1, lat1 = pyproj.transform(british, wgs84, int(coordinates[0]) , int(coordinates[1]))
        lon2, lat2 = pyproj.transform(british, wgs84, int(coordinates[2]) , int(coordinates[3]))
        print('{ '+str(lon1)+", "+str(lat1)+", "+str(lon2)+", "+str(lat2), end=' }, \n')

#lon1, lat1 = pyproj.transform(british, wgs84, 420000 , 160000)
#print('\n\n'+ str(lon1) + '   '+str(lat1))
