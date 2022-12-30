import pandas as pd
import numpy as np

# If you have not run the "Obtain_dataset.ipynb" notebook, you can download the datafile from here: https://cdn.openai.com/API/examples/data/fine_food_reviews_with_embeddings_1k.csv
datafile_path = "./docItems.csv"
df = pd.read_csv(datafile_path)
df['Score'] = pd.factorize(df['Category'])[0]

df["Embedding"] = df.Embedding.apply(eval).apply(np.array)
matrix = np.vstack(df.Embedding.values)
matrix.shape

from sklearn.cluster import KMeans

n_clusters = 5

kmeans = KMeans(n_clusters=n_clusters, init="k-means++", random_state=42, n_init='auto')
kmeans.fit(matrix)
labels = kmeans.labels_
df["Cluster"] = labels

df.groupby("Cluster").Score.mean().sort_values()

from sklearn.manifold import TSNE
import matplotlib
import matplotlib.pyplot as plt

tsne = TSNE(
    n_components=2, perplexity=15, random_state=42, init="random", learning_rate=200
)
vis_dims2 = tsne.fit_transform(matrix)

x = [x for x, y in vis_dims2]
y = [y for x, y in vis_dims2]

for category, color in enumerate( ["red",
          "darkorange",
          "gold",
          "turquoise",
          "darkgreen",
          "blue",
          "gray",
          "black",
          "aqua",
          "pink",
          "green",
          "yellow",
          "plum"]):
    xs = np.array(x)[df.Cluster == category]
    ys = np.array(y)[df.Cluster == category]
    plt.scatter(xs, ys, color=color, alpha=0.3)

    avg_x = xs.mean()
    avg_y = ys.mean()

    plt.scatter(avg_x, avg_y, marker="x", color=color, s=100)

plt.title("Roblox doc site articles")
plt.show()