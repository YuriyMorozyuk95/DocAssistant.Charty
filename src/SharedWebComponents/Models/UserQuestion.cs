﻿

namespace SharedWebComponents.Models;

public readonly record struct UserQuestion(
    string Question,
    DateTime AskedOn);
