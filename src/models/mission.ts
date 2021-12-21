import { tag } from '@equinor/eds-icons'

export class Mission {
    name: string
    link: string
    tags: Tag[]

    constructor(name: string, link: string, tags: Tag[]) {
        this.name = name
        this.link = link
        this.tags = tags
    }
}

export class Tag {
    name: string
    constructor(name: string) {
        this.name = name
    }
}

const tag1_turtlebot: Tag = new Tag('313-LD-1104')
const tag2_turtlebot: Tag = new Tag('313-LD-1111')
const tag3_turtlebot: Tag = new Tag('313-PA-101A')
const tags_turtlebot: Tag[] = [tag1_turtlebot, tag2_turtlebot, tag3_turtlebot]

const turtlebot_mission: Mission = new Mission('Turtlebot', 'https://echo.equinor.com/mp?editId=400', tags_turtlebot)

const tag1_testplan: Tag = new Tag('331-VG-003')
const tag2_testplan: Tag = new Tag('331-VG-002')
const tags_testplan: Tag[] = [tag1_testplan, tag2_testplan]
const testplan_mission: Mission = new Mission('TestPlan', 'https://echo.equinor.com/mp?editId=287', tags_testplan)

export const defaultMissions: { [name: string]: Mission } = {
    turtlebot: turtlebot_mission,
    testplan: testplan_mission,
}
