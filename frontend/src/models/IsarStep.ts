import { IsarTask } from './IsarTask'

export interface IsarStep {
    id: string
    isarStepId: string
    task: IsarTask
    tagId?: string
    stepStatus: IsarStepStatus
    stepType: StepTypeEnum
    inspectionType?: InspectionTypeEnum
    time: Date
    fileLocation?: string
}

enum IsarStepStatus {
    Successful,
    InProgress,
    NotStarted,
    Failed,
    Cancelled,
}

enum StepTypeEnum {
    DriveToPose,
    TakeImage,
    TakeVideo,
    TakeThermalImage,
    TakeThermalVideo,
    RecordAudio,
}

enum InspectionTypeEnum {
    Image = 'Image',
    ThermalImage = 'ThermalImage',
    Video = 'Video',
    ThermalVideo = 'ThermalVideo',
    Audio = 'Audio',
}
