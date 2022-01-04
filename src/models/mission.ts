import { DriveToPose, TakeImage, TakeThermalImage, Task } from './task'
import { defaultPose, defaultPosition } from './pose'

export class Mission {
    name: string
    link: string
    tags: Tag[]
    status: MissionStatus
    tasks: Task[]
    current_task: Task

    constructor(name: string, link: string, tags: Tag[], status: MissionStatus, tasks: Task[], current_task: Task) {
        this.name = name
        this.link = link
        this.tags = tags
        this.status = status
        this.tasks = tasks
        this.current_task = current_task
    }
    task_of_total_task_string(): string {
        return `${this.current_task.task_number} of ${this.tasks[this.tasks.length - 1].task_number}`
    }
}

export class Tag {
    name: string
    constructor(name: string) {
        this.name = name
    }
}

export enum MissionStatus {
    InProgress = 'In progress',
    Paused = 'Paused',
    Completed = 'Completed',
    Aborted = 'Aborted',
    Error = 'Error',
}

const tag1_turtlebot: Tag = new Tag('313-LD-1104')
const tag2_turtlebot: Tag = new Tag('313-LD-1111')
const tag3_turtlebot: Tag = new Tag('313-PA-101A')
const tags_turtlebot: Tag[] = [tag1_turtlebot, tag2_turtlebot, tag3_turtlebot]

const task1_turtlebot: Task = new DriveToPose(defaultPose, 1)
const task2_turtlebot: Task = new TakeImage(defaultPosition, 2)
const task3_turtlebot: Task = new DriveToPose(defaultPose, 3)
const task4_turtlebot: Task = new TakeImage(defaultPosition, 4)
const task5_turtlebot: Task = new TakeThermalImage(defaultPosition, 5)
const task6_turtlebot: Task = new DriveToPose(defaultPose, 6)
const tasks_turtlebot: Task[] = [
    task1_turtlebot,
    task2_turtlebot,
    task3_turtlebot,
    task4_turtlebot,
    task5_turtlebot,
    task6_turtlebot,
]

const current_task_turtlebot: Task = task4_turtlebot

const turtlebot_mission: Mission = new Mission(
    'Turtlebot',
    'https://echo.equinor.com/mp?editId=400',
    tags_turtlebot,
    MissionStatus.InProgress,
    tasks_turtlebot,
    current_task_turtlebot
)

const tag1_testplan: Tag = new Tag('331-VG-003')
const tag2_testplan: Tag = new Tag('331-VG-002')
const tags_testplan: Tag[] = [tag1_testplan, tag2_testplan]

const task1_testplan: Task = new DriveToPose(defaultPose, 1)
const task2_testplan: Task = new TakeImage(defaultPosition, 2)
const task3_testplan: Task = new DriveToPose(defaultPose, 3)
const tasks_testplan: Task[] = [task1_testplan, task2_testplan, task3_testplan]
const current_task_testplan: Task = task3_testplan

const testplan_mission: Mission = new Mission(
    'TestPlan',
    'https://echo.equinor.com/mp?editId=287',
    tags_testplan,
    MissionStatus.Paused,
    tasks_testplan,
    current_task_testplan
)

export const defaultMissions: { [name: string]: Mission } = {
    turtlebot: turtlebot_mission,
    testplan: testplan_mission,
}
