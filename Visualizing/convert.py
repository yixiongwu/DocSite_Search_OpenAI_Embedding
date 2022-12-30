import pandas as pd
df = pd.read_json("../WebApi/docItems.json")
df.to_csv('docItems.csv')