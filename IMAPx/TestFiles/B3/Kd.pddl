(define (domain Kbox-3)
(:requirements :strips :typing)
;;BestCase
(:types pos agent box push)
(:constants
 p1-1 - pos
 p1-2 - pos
 p2-1 - pos
 p2-2 - pos
 p3-1 - pos
 p3-2 - pos
 b0 - box
 b1 - box
 b2 - box
 a1 - agent
 a2 - agent
)

(:predicates
(adj ?i - pos ?j - pos)
(agent-at ?a - agent ?i - pos)
(Kbox-at ?b - box ?i - pos)
(KNbox-at ?b - box ?i - pos)
(heavy ?b - box)
(same-agent ?a1 - agent ?a2 - agent)
)

(:action move
 :parameters (?start - pos ?end - pos ?a - agent )
 :precondition 
(and (adj ?start ?end) (agent-at ?a ?start))
 :effect 
(and (not (agent-at ?a ?start)) (agent-at ?a ?end))
)
(:action push
 :parameters (?start - pos ?end - pos ?b - box ?a - agent )
 :precondition 
(and (adj ?start ?end) (agent-at ?a ?start) (not (KNbox-at ?b ?start)) (Kbox-at ?b ?start) (not (heavy ?b)))
 :effect 
(and (not (Kbox-at ?b ?start)) (KNbox-at ?b ?start) (not (KNbox-at ?b ?end)) (Kbox-at ?b ?end))
)
(:action joint-push
 :parameters (?start - pos ?end - pos ?b - box ?a1 - agent ?a2 - agent )
 :precondition 
(and (adj ?start ?end) (agent-at ?a1 ?start) (agent-at ?a2 ?start) (not (KNbox-at ?b ?start)) (Kbox-at ?b ?start) (heavy ?b) (not (same-agent ?a1 ?a2)))
 :effect 
(and (not (Kbox-at ?b ?start)) (KNbox-at ?b ?start) (not (KNbox-at ?b ?end)) (Kbox-at ?b ?end))
)
(:action observe-boxT
 :parameters (?i - pos ?a - agent ?b - box )
 :precondition 
(and (not (Kbox-at ?b ?i)) (not (KNbox-at ?b ?i)) (agent-at ?a ?i))
 :effect 
(and (Kbox-at ?b ?i))
)
(:action observe-boxF
 :parameters (?i - pos ?a - agent ?b - box )
 :precondition 
(and (not (Kbox-at ?b ?i)) (not (KNbox-at ?b ?i)) (agent-at ?a ?i))
 :effect 
(and (KNbox-at ?b ?i))
)
)
