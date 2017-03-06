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
password='Dornsife123!!'


#osm_conn = sqlite3.connect(self.sqlite3_db)
conn = psycopg2.connect(database=database, user=user, password=password)
cursor = conn.cursor()

#cursor.execute("DROP TABLE frequency")
cursor.execute('''
            CREATE TABLE IF NOT EXISTS frequency (
                    word_id bigserial PRIMARY KEY,
		    entity_id bigint[],
		    word_name text UNIQUE,
		    word_count bigserial,
		    front bigserial,
		    back bigserial)''')
cursor.execute("DELETE FROM frequency")

conn.commit()


##cursor.execute("SELECT word_name FROM frequency2 WHERE word_count>12000")
##keywords=set()
##for row in cursor.fetchall():
##    print(row[0])
##    keywords.add(row[0])
##print(len(keywords))
##print(type(keywords))
##conn.commit()


cursor.execute('''SELECT * FROM entity''')
rows=cursor.fetchall()
word_id=1
total_words=0
for row in rows:
    #print(row[0])
    #words = re.split(',| |&|-|:|\[|\]|/|\(|\)|;|\'|"|\\|.|*|+|=|!|<|%|&|_|\n|\t|\||\{|\}|?|&|~', row[1])
    words = re.findall(r"\w+",row[1])
    front = 1
    back = 0
    for word in words:
        if (word==words[0]):
            front = 1
            back = 0
        elif(word == words[len(words)-1]):
            front = 0
            back = 1        
        else:
            front= 0
            back = 0
        word=word.lower()


##        if(word in keywords):
##            cursor.execute('''UPDATE frequency2 SET word_count=word_count+1
##                        WHERE word_name = '{0}' '''.format(word))
##        else:
##            cursor.execute('''SELECT word_id FROM frequency2 WHERE word_name = '{}' '''.format(word))
##            entry = cursor.fetchall()
##            if(len(entry)==0):
##                s = '''INSERT INTO frequency2
##                                    VALUES({0},'{{{1}}}','{2}', 1)'''.format(str(word_id), row[0], word) 
##                cursor.execute(s)
##                word_id+=1
##            else:
##                cursor.execute('''UPDATE frequency2 SET entity_id = entity_id || '{{{0}}}', word_count=word_count+1
##                            WHERE word_name = '{1}' '''.format(row[0],word))



        cursor.execute('''UPDATE frequency SET entity_id = entity_id || '{{{0}}}', word_count=word_count+1, front=front+{2}, back = back+{3}
                            WHERE word_name = '{1}' '''.format(row[0],word, front, back))
        if(cursor.rowcount==0):
            s = '''INSERT INTO frequency
                                    VALUES({0},'{{{1}}}','{2}', 1, {3}, {4})'''.format(str(word_id), row[0], word, front, back) 
            cursor.execute(s)
            word_id+=1
        total_words+=1
    conn.commit()
cursor.close()
conn.close()
print(total_words)
