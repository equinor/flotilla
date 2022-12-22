import { Button, Icon } from "@equinor/eds-core-react";
import { arrow_back } from '@equinor/eds-icons'

Icon.add({ arrow_back })

export function BackButton(){
    return(
        <Button variant="ghost" href="..">
            <Icon name="arrow_back" size={32} />Back
        </Button>
    )
}