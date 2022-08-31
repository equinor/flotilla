import { Autocomplete, Button, Dialog } from "@equinor/eds-core-react";
import { useState } from "react";
import styled from "styled-components";

interface IProps {
    options: Array<string>;
}

const StyledMissionDialog = styled.div`
display: flex;
justify-content: center;
`
const StyledAutoComplete = styled.div`
display: flex;
justify-content: center;
min-width: 200px;
min-height: 200px;
`

export const ScheduleMissionDialog = (props: IProps): JSX.Element => {
    const [isOpen, setIsOpen] = useState<boolean>(false);
    return (
        <>

            <Button onClick={() => { setIsOpen(true) }}>Schedule Mission</Button>
            <StyledMissionDialog>
                <Dialog open={isOpen} isDismissable>
                    <StyledAutoComplete>
                        <Autocomplete options={props.options} label={"Schedule Missions"}></Autocomplete>
                    </StyledAutoComplete>
                    <Button onClick={() => { setIsOpen(false) }}> Close </Button>
                </Dialog>
            </StyledMissionDialog>
        </>
    )
}