import numpy as np
from sklearn.linear_model import LinearRegression

# Example data
X = np.array([[1], [2], [3], [4], [5]])  # Feature (independent variable)
y = np.array([2, 4, 6, 8, 10])           # Target (dependent variable)

# Create and fit the model
model = LinearRegression()
model.fit(X, y)

# Coefficient and intercept
print(f"Slope (coef_): {model.coef_[0]}")
print(f"Intercept (intercept_): {model.intercept_}")

# Predict new values
X_new = np.array([[6], [7]])
y_pred = model.predict(X_new)
print(f"Predictions for {X_new.flatten()}: {y_pred}")