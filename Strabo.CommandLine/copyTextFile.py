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
            CREATE TABLE IF NOT EXISTS text_file (
		    word_name text UNIQUE,
                    word_length int,
        	)''')
cursor.execute('''DELETE from text_file''')
conn.commit()

with open('dict_eng_ordinance_survey.txt') as f:
    for line in f:
        cursor.execute('''INSERT INTO text_file VALUES({0},{1})'''.format(str(len(line)), line))
        conn.commit()

cursor.close()
conn.close()
