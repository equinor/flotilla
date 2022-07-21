import { IsarTask } from "./IsarTask"

export interface IsarStep {
    id: string
    IsarStepId: string 
    Task: IsarTask 
    TagId: string 
    StepStatus: IsarStepStatus 
    StepType: StepTypeEnum 
    InspectionType: InspectionTypeEnum 
    Time: Date 
    FileLocation: string 
}

enum IsarStepStatus{
    Successful,
    InProgress,
    NotStarted,
    Failed,
    Cancelled
}

enum StepTypeEnum{
    DriveToPose,
    TakeImage,
    TakeVideo,
    TakeThermalImage,
    TakeThermalVideo,
    RecordAudio
}

enum InspectionTypeEnum{
    Image,
    ThermalImage,
    Video,
    ThermalVideo,
    Audio
}