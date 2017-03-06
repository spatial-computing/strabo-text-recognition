from lxml import etree
import bz2
import sys
import json
import time
import os
import sqlite3
import psycopg2
import re


database='dictionary'
user='postgres'
password='@ahf143!!'


#osm_conn = sqlite3.connect(self.sqlite3_db)
conn = psycopg2.connect(database=database, user=user, password=password)
cursor = conn.cursor()

cursor.execute("SELECT word_count FROM frequency2")
counter=0
for row in cursor.fetchall():
    counter+=int(row[0])
conn.commit()
print(counter)


cursor.execute("SELECT word_count FROM frequency2 WHERE word_count>10000")
counter=0
for row in cursor.fetchall():
    counter+=int(row[0])
conn.commit()
print(counter)

cursor.execute("SELECT word_count FROM frequency2 WHERE word_count>12000")
counter=0
for row in cursor.fetchall():
    counter+=int(row[0])
conn.commit()
print(counter)


cursor.execute("SELECT word_count FROM frequency2 WHERE word_count>15000")
counter=0
for row in cursor.fetchall():
    counter+=int(row[0])
conn.commit()
print(counter)



cursor.execute("SELECT word_count FROM frequency2 WHERE word_count<12000 AND word_count>1000")
counter=0
for row in cursor.fetchall():
    counter+=int(row[0])
conn.commit()
print(counter)


try:
    #cursor.execute('''INSERT INTO frequency2 VALUES(-1,'{{-1}}', 'tolololololol23324' , 1)''') 

    cursor.execute('''DELETE FROM frequency2 WHERE word_name = 'tolololololol23324' ''')
    cursor.execute('''UPDATE frequency2 SET word_count=word_count+1
                            WHERE word_name = 'tolololololol23324' ''')
    print(cursor.rowcount)
except:
    print("GOTCHA")
                

