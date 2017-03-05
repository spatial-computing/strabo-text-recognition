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

cursor.execute('''
            CREATE TABLE IF NOT EXISTS text_file(
                    word_id bigserial PRIMARY KEY, 
		    word_name text,
                    word_length int)''')
cursor.execute('''DELETE from text_file''')
conn.commit()
word_id=1
with open('dict_eng_ordnance_survey.txt') as f:
    for line in f:
        line = line.strip()
        cursor.execute('''INSERT INTO text_file VALUES({2},'{1}',{0})'''.format(str(len(line)), line, str(word_id)))
        word_id+=1
        conn.commit()

cursor.close()
conn.close()
